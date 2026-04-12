using System.Text.Json;
using System.Web;
using Microsoft.Extensions.Options;
using ThreeBooks.BookBackend.LoginService.Api.ErrorHandling;
using ThreeBooks.BookBackend.LoginService.Application.Abstractions;
using ThreeBooks.BookBackend.LoginService.Contracts.WeChat;
using ThreeBooks.BookBackend.LoginService.Domain.Enums;
using ThreeBooks.BookBackend.LoginService.Options;

namespace ThreeBooks.BookBackend.LoginService.Infrastructure.External.WeChat;

public sealed class WeChatAuthClient(
    HttpClient httpClient,
    IOptions<WeChatOpenPlatformOptions> openPlatformOptionsAccessor,
    IOptions<WeChatOfficialAccountOptions> officialAccountOptionsAccessor) : IWeChatAuthClient
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly WeChatOpenPlatformOptions _openPlatformOptions = openPlatformOptionsAccessor.Value;
    private readonly WeChatOfficialAccountOptions _officialAccountOptions = officialAccountOptionsAccessor.Value;

    public string BuildAuthorizeUrl(WeChatAuthorizeRequest request)
    {
        var baseUrl = request.Provider == AuthProvider.WeChatQr
            ? "https://open.weixin.qq.com/connect/qrconnect"
            : "https://open.weixin.qq.com/connect/oauth2/authorize";

        var scope = request.Provider == AuthProvider.WeChatQr
            ? "snsapi_login"
            : request.RequireUserInfoScope
                ? "snsapi_userinfo"
                : "snsapi_base";

        var query = HttpUtility.ParseQueryString(string.Empty);
        query["appid"] = request.AppId;
        query["redirect_uri"] = request.RedirectUri;
        query["response_type"] = "code";
        query["scope"] = scope;
        query["state"] = request.State;

        return $"{baseUrl}?{query}#wechat_redirect";
    }

    public async Task<WeChatIdentityProfile> ExchangeCodeForProfileAsync(AuthProvider provider, string code, CancellationToken cancellationToken)
    {
        var (appId, appSecret) = GetCredentials(provider);
        if (string.IsNullOrWhiteSpace(appId) || string.IsNullOrWhiteSpace(appSecret))
        {
            throw new ApiException(StatusCodes.Status503ServiceUnavailable, $"{provider} is not configured.");
        }

        var tokenUrl = $"https://api.weixin.qq.com/sns/oauth2/access_token?appid={Uri.EscapeDataString(appId)}&secret={Uri.EscapeDataString(appSecret)}&code={Uri.EscapeDataString(code)}&grant_type=authorization_code";
        var tokenJson = await _httpClient.GetStringAsync(tokenUrl, cancellationToken);
        using var tokenDocument = JsonDocument.Parse(tokenJson);
        EnsureNoWeChatError(tokenDocument.RootElement);

        var openId = GetRequiredString(tokenDocument.RootElement, "openid");
        var accessToken = GetRequiredString(tokenDocument.RootElement, "access_token");
        var unionId = GetOptionalString(tokenDocument.RootElement, "unionid");

        string? nickname = null;
        string? avatarUrl = null;
        string rawProfileJson = tokenJson;

        try
        {
            var userInfoUrl = $"https://api.weixin.qq.com/sns/userinfo?access_token={Uri.EscapeDataString(accessToken)}&openid={Uri.EscapeDataString(openId)}&lang=zh_CN";
            var userInfoJson = await _httpClient.GetStringAsync(userInfoUrl, cancellationToken);
            using var userInfoDocument = JsonDocument.Parse(userInfoJson);
            EnsureNoWeChatError(userInfoDocument.RootElement);

            nickname = GetOptionalString(userInfoDocument.RootElement, "nickname");
            avatarUrl = GetOptionalString(userInfoDocument.RootElement, "headimgurl");
            unionId ??= GetOptionalString(userInfoDocument.RootElement, "unionid");
            rawProfileJson = userInfoJson;
        }
        catch (Exception) when (provider == AuthProvider.WeChatOfficialAccount || provider == AuthProvider.WeChatQr)
        {
            // Token exchange still gives us enough data to persist an external principal.
        }

        return new WeChatIdentityProfile(provider, appId, openId, unionId, nickname, avatarUrl, rawProfileJson);
    }

    private (string AppId, string AppSecret) GetCredentials(AuthProvider provider) => provider switch
    {
        AuthProvider.WeChatQr => (_openPlatformOptions.AppId, _openPlatformOptions.AppSecret),
        AuthProvider.WeChatOfficialAccount => (_officialAccountOptions.AppId, _officialAccountOptions.AppSecret),
        _ => throw new ApiException(StatusCodes.Status400BadRequest, $"Unsupported provider '{provider}'.")
    };

    private static string GetRequiredString(JsonElement element, string propertyName)
    {
        var value = GetOptionalString(element, propertyName);
        return string.IsNullOrWhiteSpace(value)
            ? throw new ApiException(StatusCodes.Status502BadGateway, $"WeChat response did not contain '{propertyName}'.")
            : value;
    }

    private static string? GetOptionalString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private static void EnsureNoWeChatError(JsonElement element)
    {
        if (!element.TryGetProperty("errcode", out var errorCode) || errorCode.ValueKind != JsonValueKind.Number)
        {
            return;
        }

        var errCode = errorCode.GetInt32();
        if (errCode == 0)
        {
            return;
        }

        var errMessage = GetOptionalString(element, "errmsg") ?? "Unknown WeChat error.";
        throw new ApiException(StatusCodes.Status502BadGateway, $"WeChat API error {errCode}: {errMessage}");
    }
}