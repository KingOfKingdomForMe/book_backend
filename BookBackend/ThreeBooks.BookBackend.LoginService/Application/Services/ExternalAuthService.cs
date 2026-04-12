using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ThreeBooks.BookBackend.LoginService.Api.ErrorHandling;
using ThreeBooks.BookBackend.LoginService.Application.Abstractions;
using ThreeBooks.BookBackend.LoginService.Contracts.Requests;
using ThreeBooks.BookBackend.LoginService.Contracts.Responses;
using ThreeBooks.BookBackend.LoginService.Contracts.WeChat;
using ThreeBooks.BookBackend.LoginService.Domain.Entities;
using ThreeBooks.BookBackend.LoginService.Domain.Enums;
using ThreeBooks.BookBackend.LoginService.Infrastructure.Persistence;
using ThreeBooks.BookBackend.LoginService.Options;

namespace ThreeBooks.BookBackend.LoginService.Application.Services;

public sealed class ExternalAuthService(
    LoginDbContext dbContext,
    IAuthService authService,
    IWeChatAuthClient weChatAuthClient,
    IOptions<ExternalAuthOptions> externalAuthOptionsAccessor,
    IOptions<WeChatOpenPlatformOptions> openPlatformOptionsAccessor,
    IOptions<WeChatOfficialAccountOptions> officialAccountOptionsAccessor) : IExternalAuthService
{
    private static readonly AuthProvider[] WeChatProviders = [AuthProvider.WeChatQr, AuthProvider.WeChatOfficialAccount];

    private readonly LoginDbContext _dbContext = dbContext;
    private readonly IAuthService _authService = authService;
    private readonly IWeChatAuthClient _weChatAuthClient = weChatAuthClient;
    private readonly ExternalAuthOptions _externalAuthOptions = externalAuthOptionsAccessor.Value;
    private readonly WeChatOpenPlatformOptions _openPlatformOptions = openPlatformOptionsAccessor.Value;
    private readonly WeChatOfficialAccountOptions _officialAccountOptions = officialAccountOptionsAccessor.Value;

    public async Task<StartExternalAuthResponse> StartWeChatQrLoginAsync(
        StartWeChatLoginRequest request,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        EnsureProviderConfigured(AuthProvider.WeChatQr);

        var (session, completionToken) = await CreateSessionAsync(
            AuthProvider.WeChatQr,
            ExternalAuthPurpose.Login,
            requestedByUserId: null,
            request.RedirectUri,
            cancellationToken);

        var authorizeUrl = _weChatAuthClient.BuildAuthorizeUrl(new WeChatAuthorizeRequest(
            AuthProvider.WeChatQr,
            _openPlatformOptions.AppId,
            _openPlatformOptions.CallbackUrl,
            session.State,
            RequireUserInfoScope: true));

        return new StartExternalAuthResponse(session.Id, session.Provider, session.Purpose, authorizeUrl, session.ExpiresAtUtc, session.State, completionToken);
    }

    public async Task<StartExternalAuthResponse> StartWeChatOfficialAccountBindAsync(
        Guid currentUserId,
        StartWeChatLoginRequest request,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        EnsureProviderConfigured(AuthProvider.WeChatOfficialAccount);

        var userExists = await _dbContext.Users.AnyAsync(entity => entity.Id == currentUserId, cancellationToken);
        if (!userExists)
        {
            throw new ApiException(StatusCodes.Status404NotFound, "User was not found.");
        }

        var (session, completionToken) = await CreateSessionAsync(
            AuthProvider.WeChatOfficialAccount,
            ExternalAuthPurpose.Bind,
            currentUserId,
            request.RedirectUri,
            cancellationToken);

        var authorizeUrl = _weChatAuthClient.BuildAuthorizeUrl(new WeChatAuthorizeRequest(
            AuthProvider.WeChatOfficialAccount,
            _officialAccountOptions.AppId,
            _officialAccountOptions.CallbackUrl,
            session.State,
            _officialAccountOptions.RequireUserInfoScope));

        return new StartExternalAuthResponse(session.Id, session.Provider, session.Purpose, authorizeUrl, session.ExpiresAtUtc, session.State, completionToken);
    }

    public async Task<ExternalAuthCallbackResponse> HandleWeChatCallbackAsync(
        AuthProvider provider,
        ExternalAuthCallbackRequest request,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        if (!WeChatProviders.Contains(provider))
        {
            throw new ApiException(StatusCodes.Status400BadRequest, $"Unsupported provider '{provider}'.");
        }

        if (string.IsNullOrWhiteSpace(request.State))
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "WeChat callback state is required.");
        }

        var session = await _dbContext.ExternalAuthSessions
            .FirstOrDefaultAsync(entity => entity.State == request.State, cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "External auth session was not found.");

        if (session.Provider != provider)
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "External auth session provider does not match the callback provider.");
        }

        if (session.ExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            session.Status = ExternalAuthSessionStatus.Expired;
            session.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);

            return BuildCallbackResponse(session, "The authorization session has expired.", requiresBinding: false, authentication: null, completionToken: null);
        }

        if (!string.IsNullOrWhiteSpace(request.Error))
        {
            session.Status = ExternalAuthSessionStatus.Failed;
            session.FailureCode = request.Error;
            session.FailureMessage = request.ErrorDescription ?? "WeChat authorization was cancelled or failed.";
            session.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);

            return BuildCallbackResponse(session, session.FailureMessage, requiresBinding: false, authentication: null, completionToken: null);
        }

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "WeChat callback code is required.");
        }

        var profile = await _weChatAuthClient.ExchangeCodeForProfileAsync(provider, request.Code, cancellationToken);
        ApplyExternalPrincipal(session, profile);

        if (session.Purpose == ExternalAuthPurpose.Bind)
        {
            if (!session.RequestedByUserId.HasValue)
            {
                throw new ApiException(StatusCodes.Status400BadRequest, "Bind session does not contain a target user.");
            }

            await UpsertExternalIdentityAsync(session.RequestedByUserId.Value, profile, cancellationToken);
            session.ResolvedUserId = session.RequestedByUserId;
            session.Status = ExternalAuthSessionStatus.Bound;
            session.CompletedAtUtc = DateTimeOffset.UtcNow;
            session.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await AddAuditLogAsync(session.RequestedByUserId.Value, $"auth.wechat.{provider}.bound", $"Bound external identity '{profile.OpenId}' to user.", context, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return BuildCallbackResponse(session, "WeChat identity has been bound to the current user.", requiresBinding: false, authentication: null, completionToken: null);
        }

        var resolvedUserId = await ResolveUserIdAsync(profile, cancellationToken);
        if (resolvedUserId.HasValue)
        {
            await UpsertExternalIdentityAsync(resolvedUserId.Value, profile, cancellationToken, updateLastLogin: true);
            session.ResolvedUserId = resolvedUserId.Value;
            session.Status = ExternalAuthSessionStatus.Completed;
            session.CompletedAtUtc = DateTimeOffset.UtcNow;
            session.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);

            return BuildCallbackResponse(session, "WeChat authorization completed. Call the completion endpoint to issue tokens.", requiresBinding: false, authentication: null, completionToken: null);
        }

        session.Status = ExternalAuthSessionStatus.Authorized;
        session.AuthorizedAtUtc = DateTimeOffset.UtcNow;
        session.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return BuildCallbackResponse(session, "WeChat account is authorized but not yet bound to a local account.", requiresBinding: true, authentication: null, completionToken: null);
    }

    public async Task<ExternalAuthSessionResponse> GetSessionStatusAsync(Guid sessionId, string completionToken, CancellationToken cancellationToken)
    {
        var session = await GetValidatedSessionAsync(sessionId, completionToken, cancellationToken);
        return MapSession(session, completionToken);
    }

    public async Task<AuthenticationResponse> CompleteLoginAsync(
        Guid sessionId,
        string completionToken,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        var session = await GetValidatedSessionAsync(sessionId, completionToken, cancellationToken);
        if (session.Purpose != ExternalAuthPurpose.Login)
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "Only login sessions can be completed.");
        }

        if (session.Status != ExternalAuthSessionStatus.Completed)
        {
            throw new ApiException(StatusCodes.Status409Conflict, "The external login session is not ready to complete.");
        }

        if (!session.ResolvedUserId.HasValue)
        {
            throw new ApiException(StatusCodes.Status409Conflict, "The external login session has no resolved user.");
        }

        session.Status = ExternalAuthSessionStatus.Consumed;
        session.ConsumedAtUtc = DateTimeOffset.UtcNow;
        session.UpdatedAtUtc = DateTimeOffset.UtcNow;

        var authentication = await _authService.SignInUserAsync(session.ResolvedUserId.Value, context, $"auth.wechat.{session.Provider}.login", cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return authentication;
    }

    public async Task<ExternalAuthSessionResponse> BindCurrentUserAsync(
        Guid sessionId,
        string completionToken,
        Guid currentUserId,
        CancellationToken cancellationToken)
    {
        var session = await GetValidatedSessionAsync(sessionId, completionToken, cancellationToken);
        if (session.Purpose != ExternalAuthPurpose.Login)
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "Only login sessions can be bound through this endpoint.");
        }

        if (session.Status != ExternalAuthSessionStatus.Authorized)
        {
            throw new ApiException(StatusCodes.Status409Conflict, "The external auth session is not waiting for binding.");
        }

        if (string.IsNullOrWhiteSpace(session.ExternalOpenId) || string.IsNullOrWhiteSpace(session.AppId))
        {
            throw new ApiException(StatusCodes.Status409Conflict, "The external auth session does not contain a resolved external identity.");
        }

        var profile = new WeChatIdentityProfile(
            session.Provider,
            session.AppId,
            session.ExternalOpenId,
            session.ExternalUnionId,
            session.ExternalNickname,
            session.ExternalAvatarUrl,
            session.ExternalNickname ?? string.Empty);

        await UpsertExternalIdentityAsync(currentUserId, profile, cancellationToken, updateLastLogin: true);
        session.ResolvedUserId = currentUserId;
        session.Status = ExternalAuthSessionStatus.Completed;
        session.CompletedAtUtc = DateTimeOffset.UtcNow;
        session.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapSession(session, completionToken);
    }

    private async Task<(ExternalAuthSession Session, string CompletionToken)> CreateSessionAsync(
        AuthProvider provider,
        ExternalAuthPurpose purpose,
        Guid? requestedByUserId,
        string? redirectUri,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var state = CreateOpaqueToken(32);
        var nonce = CreateOpaqueToken(24);
        var completionToken = CreateOpaqueToken(32);
        var session = new ExternalAuthSession
        {
            Id = Guid.NewGuid(),
            Provider = provider,
            Purpose = purpose,
            Status = ExternalAuthSessionStatus.Pending,
            State = state,
            Nonce = nonce,
            RequestedByUserId = requestedByUserId,
            RedirectUri = string.IsNullOrWhiteSpace(redirectUri) ? null : redirectUri.Trim(),
            ExpiresAtUtc = now.AddMinutes(_externalAuthOptions.SessionLifetimeMinutes),
            CompletionTokenHash = ComputeHash(completionToken),
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            AppId = GetAppId(provider)
        };

        _dbContext.ExternalAuthSessions.Add(session);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return (session, completionToken);
    }

    private async Task<ExternalAuthSession> GetValidatedSessionAsync(Guid sessionId, string completionToken, CancellationToken cancellationToken)
    {
        var session = await _dbContext.ExternalAuthSessions
            .FirstOrDefaultAsync(entity => entity.Id == sessionId, cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "External auth session was not found.");

        if (string.IsNullOrWhiteSpace(completionToken) || !string.Equals(session.CompletionTokenHash, ComputeHash(completionToken), StringComparison.Ordinal))
        {
            throw new ApiException(StatusCodes.Status401Unauthorized, "Invalid completion token.");
        }

        if (session.ExpiresAtUtc <= DateTimeOffset.UtcNow && session.Status is not ExternalAuthSessionStatus.Completed and not ExternalAuthSessionStatus.Bound and not ExternalAuthSessionStatus.Consumed)
        {
            session.Status = ExternalAuthSessionStatus.Expired;
            session.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            throw new ApiException(StatusCodes.Status410Gone, "The external auth session has expired.");
        }

        return session;
    }

    private async Task<Guid?> ResolveUserIdAsync(WeChatIdentityProfile profile, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(profile.UnionId))
        {
            var matchedByUnionId = await _dbContext.ExternalIdentities
                .Where(entity => WeChatProviders.Contains(entity.Provider) && entity.UnionId == profile.UnionId)
                .Select(entity => entity.UserId)
                .FirstOrDefaultAsync(cancellationToken);

            if (matchedByUnionId != Guid.Empty)
            {
                return matchedByUnionId;
            }
        }

        return await _dbContext.ExternalIdentities
            .Where(entity => entity.Provider == profile.Provider && entity.AppId == profile.AppId && entity.OpenId == profile.OpenId)
            .Select(entity => (Guid?)entity.UserId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task UpsertExternalIdentityAsync(
        Guid userId,
        WeChatIdentityProfile profile,
        CancellationToken cancellationToken,
        bool updateLastLogin = false)
    {
        ExternalIdentity? identity = await _dbContext.ExternalIdentities
            .FirstOrDefaultAsync(entity => entity.Provider == profile.Provider && entity.AppId == profile.AppId && entity.OpenId == profile.OpenId, cancellationToken);

        if (identity is null && !string.IsNullOrWhiteSpace(profile.UnionId))
        {
            identity = await _dbContext.ExternalIdentities
                .FirstOrDefaultAsync(entity => WeChatProviders.Contains(entity.Provider) && entity.UnionId == profile.UnionId, cancellationToken);
        }

        if (identity is not null && identity.UserId != userId)
        {
            throw new ApiException(StatusCodes.Status409Conflict, "This WeChat identity has already been bound to another local user.");
        }

        var now = DateTimeOffset.UtcNow;
        if (identity is null)
        {
            identity = new ExternalIdentity
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Provider = profile.Provider,
                AppId = profile.AppId,
                OpenId = profile.OpenId,
                CreatedAtUtc = now
            };

            _dbContext.ExternalIdentities.Add(identity);
        }

        identity.UserId = userId;
        identity.Provider = profile.Provider;
        identity.AppId = profile.AppId;
        identity.OpenId = profile.OpenId;
        identity.UnionId = profile.UnionId;
        identity.Nickname = profile.Nickname;
        identity.AvatarUrl = profile.AvatarUrl;
        identity.RawProfileJson = profile.RawProfileJson;
        identity.UpdatedAtUtc = now;
        if (updateLastLogin)
        {
            identity.LastLoginAtUtc = now;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private void ApplyExternalPrincipal(ExternalAuthSession session, WeChatIdentityProfile profile)
    {
        session.AppId = profile.AppId;
        session.ExternalOpenId = profile.OpenId;
        session.ExternalUnionId = profile.UnionId;
        session.ExternalNickname = profile.Nickname;
        session.ExternalAvatarUrl = profile.AvatarUrl;
        session.AuthorizedAtUtc = DateTimeOffset.UtcNow;
        session.UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private async Task AddAuditLogAsync(Guid userId, string action, string detail, RequestContext context, CancellationToken cancellationToken)
    {
        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Action = action,
            Detail = detail,
            IpAddress = context.IpAddress,
            UserAgent = context.UserAgent,
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private ExternalAuthCallbackResponse BuildCallbackResponse(
        ExternalAuthSession session,
        string message,
        bool requiresBinding,
        AuthenticationResponse? authentication,
        string? completionToken) =>
        new(
            session.Id,
            session.Provider,
            session.Status,
            message,
            requiresBinding,
            authentication,
            MapPrincipal(session),
            completionToken);

    private ExternalAuthSessionResponse MapSession(ExternalAuthSession session, string? completionToken) =>
        new(
            session.Id,
            session.Purpose,
            session.Provider,
            session.Status,
            session.ExpiresAtUtc,
            session.Status == ExternalAuthSessionStatus.Authorized && !session.ResolvedUserId.HasValue,
            session.FailureCode,
            session.FailureMessage,
            session.ResolvedUserId,
            MapPrincipal(session),
            completionToken);

    private static ExternalPrincipalResponse? MapPrincipal(ExternalAuthSession session)
    {
        if (string.IsNullOrWhiteSpace(session.ExternalOpenId) && string.IsNullOrWhiteSpace(session.ExternalUnionId))
        {
            return null;
        }

        return new ExternalPrincipalResponse(
            session.Provider,
            session.AppId,
            session.ExternalOpenId,
            session.ExternalUnionId,
            session.ExternalNickname,
            session.ExternalAvatarUrl);
    }

    private void EnsureProviderConfigured(AuthProvider provider)
    {
        switch (provider)
        {
            case AuthProvider.WeChatQr when (!_openPlatformOptions.Enabled || string.IsNullOrWhiteSpace(_openPlatformOptions.AppId) || string.IsNullOrWhiteSpace(_openPlatformOptions.AppSecret) || string.IsNullOrWhiteSpace(_openPlatformOptions.CallbackUrl)):
                throw new ApiException(StatusCodes.Status503ServiceUnavailable, "WeChat Open Platform login is not configured.");
            case AuthProvider.WeChatOfficialAccount when (!_officialAccountOptions.Enabled || string.IsNullOrWhiteSpace(_officialAccountOptions.AppId) || string.IsNullOrWhiteSpace(_officialAccountOptions.AppSecret) || string.IsNullOrWhiteSpace(_officialAccountOptions.CallbackUrl)):
                throw new ApiException(StatusCodes.Status503ServiceUnavailable, "WeChat Official Account binding is not configured.");
        }
    }

    private string GetAppId(AuthProvider provider) => provider switch
    {
        AuthProvider.WeChatQr => _openPlatformOptions.AppId,
        AuthProvider.WeChatOfficialAccount => _officialAccountOptions.AppId,
        _ => string.Empty
    };

    private static string CreateOpaqueToken(int byteLength)
    {
        Span<byte> buffer = stackalloc byte[byteLength];
        RandomNumberGenerator.Fill(buffer);
        return Convert.ToBase64String(buffer).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    private static string ComputeHash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }
}