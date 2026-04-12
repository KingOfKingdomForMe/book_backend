using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ThreeBooks.BookBackend.LoginService.Api.ErrorHandling;
using ThreeBooks.BookBackend.LoginService.Application.Abstractions;
using ThreeBooks.BookBackend.LoginService.Application.Services;
using ThreeBooks.BookBackend.LoginService.Domain.Entities;
using ThreeBooks.BookBackend.LoginService.Infrastructure.External.WeChat;
using ThreeBooks.BookBackend.LoginService.Infrastructure.Persistence;
using ThreeBooks.BookBackend.LoginService.Infrastructure.Seeding;
using ThreeBooks.BookBackend.LoginService.Options;

using LoginLockoutOptions = ThreeBooks.BookBackend.LoginService.Options.LockoutOptions;

namespace ThreeBooks.BookBackend.LoginService.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLoginService(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();
        services.AddProblemDetails();
        services.AddExceptionHandler<ApiExceptionHandler>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<LoginDbOptions>(configuration.GetSection(LoginDbOptions.SectionName));
        services.Configure<PasswordPolicyOptions>(configuration.GetSection(PasswordPolicyOptions.SectionName));
        services.Configure<LoginLockoutOptions>(configuration.GetSection(LoginLockoutOptions.SectionName));
        services.Configure<SeedAdminOptions>(configuration.GetSection(SeedAdminOptions.SectionName));
        services.Configure<ExternalAuthOptions>(configuration.GetSection(ExternalAuthOptions.SectionName));
        services.Configure<WeChatOpenPlatformOptions>(configuration.GetSection(WeChatOpenPlatformOptions.SectionName));
        services.Configure<WeChatOfficialAccountOptions>(configuration.GetSection(WeChatOfficialAccountOptions.SectionName));

        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey) || jwtOptions.SigningKey.Length < 32)
        {
            throw new InvalidOperationException("Jwt:SigningKey must be configured with at least 32 characters.");
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey));

        services.AddDbContext<LoginDbContext>((serviceProvider, options) =>
        {
            var connectionString = configuration.GetConnectionString("LoginDb")
                ?? throw new InvalidOperationException("ConnectionStrings:LoginDb is required.");

            options.UseMySql(
                connectionString,
                new MySqlServerVersion(new Version(8, 0, 36)),
                mysqlOptions => mysqlOptions.MigrationsAssembly(typeof(LoginDbContext).Assembly.FullName));

            var dbOptions = serviceProvider.GetRequiredService<IOptions<LoginDbOptions>>().Value;
            if (dbOptions.EnableDetailedErrors)
            {
                options.EnableDetailedErrors();
            }
        });

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = signingKey,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    NameClaimType = ClaimTypes.Name,
                    RoleClaimType = ClaimTypes.Role
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthorizationPolicies.AdminOnly, policy => policy.RequireRole(SystemRoles.Admin));
        });

        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IExternalAuthService, ExternalAuthService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<LoginDbSeeder>();
        services.AddHttpClient<IWeChatAuthClient, WeChatAuthClient>();

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "ThreeBooks BookBackend Login Service",
                Version = "v1",
                Description = "Authentication and account management API for ThreeBooks.BookBackend."
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "Input a JWT access token using the Bearer scheme.",
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                BearerFormat = "JWT",
                Scheme = "bearer"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    []
                }
            });
        });

        return services;
    }
}