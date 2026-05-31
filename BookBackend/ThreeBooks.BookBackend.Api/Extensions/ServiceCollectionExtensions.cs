using System.Security.Claims;
using System.Text;
using System.Reflection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Net.Http.Headers;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ThreeBooks.BookBackend.Api.Authorization;
using ThreeBooks.BookBackend.Application.Modules.AlbumTemplates.Interfaces;
using ThreeBooks.BookBackend.Application.Modules.AlbumTemplates.Services;
using ThreeBooks.BookBackend.Application.Modules.Albums.Interfaces;
using ThreeBooks.BookBackend.Application.Modules.Albums.Services;
using ThreeBooks.BookBackend.Application.Modules.Catalogs.Interfaces;
using ThreeBooks.BookBackend.Application.Modules.Catalogs.Services;
using ThreeBooks.BookBackend.Application.Modules.DefaultAlbums.Interfaces;
using ThreeBooks.BookBackend.Application.Modules.DefaultAlbums.Services;
using ThreeBooks.BookBackend.Application.Modules.CoverTemplates.Interfaces;
using ThreeBooks.BookBackend.Application.Modules.CoverTemplates.Services;
using ThreeBooks.BookBackend.Application.Modules.Files.Interfaces;
using ThreeBooks.BookBackend.Application.Modules.Files.Services;
using ThreeBooks.BookBackend.Application.Modules.Orders.Interfaces;
using ThreeBooks.BookBackend.Application.Modules.Orders.Services;
using ThreeBooks.BookBackend.Application.Modules.Unboxings.Interfaces;
using ThreeBooks.BookBackend.Application.Modules.Unboxings.Services;
using ThreeBooks.BookBackend.Infrastructure.Persistence.Files;
using ThreeBooks.BookBackend.Infrastructure.Persistence.Queries.AlbumTemplates;
using ThreeBooks.BookBackend.Infrastructure.Persistence.Queries.Albums;
using ThreeBooks.BookBackend.Infrastructure.Persistence.Queries.Catalogs;
using ThreeBooks.BookBackend.Infrastructure.Persistence.Queries.DefaultAlbums;
using ThreeBooks.BookBackend.Infrastructure.Persistence.Queries.Files;
using ThreeBooks.BookBackend.Infrastructure.Persistence.Queries.Orders;
using ThreeBooks.BookBackend.Infrastructure.Persistence.Queries.Unboxings;
using ThreeBooks.BookBackend.Infrastructure.Storage;
using ThreeBooks.BookBackend.Infrastructure.Storage.Options;

namespace ThreeBooks.BookBackend.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBookBackendApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();

        services.Configure<ApiJwtOptions>(configuration.GetSection(ApiJwtOptions.SectionName));
        services.Configure<DevelopmentSwaggerBearerOptions>(configuration.GetSection(DevelopmentSwaggerBearerOptions.SectionName));

        var jwtOptions = configuration.GetSection(ApiJwtOptions.SectionName).Get<ApiJwtOptions>() ?? new ApiJwtOptions();
        var developmentSwaggerBearerOptions = configuration.GetSection(DevelopmentSwaggerBearerOptions.SectionName).Get<DevelopmentSwaggerBearerOptions>() ?? new DevelopmentSwaggerBearerOptions();
        if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey) || jwtOptions.SigningKey.Length < 32)
        {
            throw new InvalidOperationException("Jwt:SigningKey must be configured with at least 32 characters.");
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey));

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

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (!developmentSwaggerBearerOptions.IsEnabled)
                        {
                            return Task.CompletedTask;
                        }

                        var authorizationHeader = context.Request.Headers[HeaderNames.Authorization].ToString();
                        if (!authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                        {
                            return Task.CompletedTask;
                        }

                        var providedToken = authorizationHeader["Bearer ".Length..].Trim();
                        if (!string.Equals(providedToken, developmentSwaggerBearerOptions.Token.Trim(), StringComparison.Ordinal))
                        {
                            return Task.CompletedTask;
                        }

                        context.Token = DevelopmentSwaggerBearerTokenFactory.CreateJwt(jwtOptions, developmentSwaggerBearerOptions);
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(
                BookBackendApiAuthorizationPolicies.OrderManage,
                policy => policy.RequireAssertion(context =>
                    BookBackendApiAuthorizationEvaluator.IsAdminOrHasAnyPermission(
                        context.User,
                        BookBackendApiPermissions.OrderManage)));

            options.AddPolicy(
                BookBackendApiAuthorizationPolicies.UnboxingModerate,
                policy => policy.RequireAssertion(context =>
                    BookBackendApiAuthorizationEvaluator.IsAdminOrHasAnyPermission(
                        context.User,
                        BookBackendApiPermissions.UnboxingModerate)));

            options.AddPolicy(
                BookBackendApiAuthorizationPolicies.TemplateManage,
                policy => policy.RequireAssertion(context =>
                    BookBackendApiAuthorizationEvaluator.IsAdminOrHasAnyPermission(
                        context.User,
                        BookBackendApiPermissions.TemplateManage)));
        });

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "ThreeBooks BookBackend API",
                Version = "v1",
                Description = "BookBackend catalog, order, album, template, file, and admin APIs."
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = DevelopmentSwaggerBearerTokenFactory.BuildSwaggerSecurityDescription(
                    "Input a JWT access token using the Bearer scheme.",
                    developmentSwaggerBearerOptions),
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

            IncludeXmlComments(options, Assembly.GetExecutingAssembly());
            IncludeXmlComments(options, typeof(ThreeBooks.BookBackend.Contracts.Common.PagedResult<>).Assembly);
        });

        services.AddScoped<IAlbumTemplateService, AlbumTemplateService>();
        services.AddScoped<IAlbumTemplateQueryStore>(_ => new AlbumTemplateQueryStore(
            configuration.GetConnectionString("BookBackendDb")
            ?? throw new InvalidOperationException("ConnectionStrings:BookBackendDb is required.")));
        services.AddScoped<IDefaultAlbumService, DefaultAlbumService>();
        services.AddScoped<IDefaultAlbumQueryStore>(_ => new DefaultAlbumQueryStore(
            configuration.GetConnectionString("BookBackendDb")
            ?? throw new InvalidOperationException("ConnectionStrings:BookBackendDb is required.")));
        services.AddScoped<ICoverTemplateService, CoverTemplateService>();

        services.AddScoped<IAlbumService, AlbumService>();
        services.AddScoped<IAlbumQueryStore>(_ => new AlbumQueryStore(
            configuration.GetConnectionString("BookBackendDb")
            ?? throw new InvalidOperationException("ConnectionStrings:BookBackendDb is required.")));

        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IOrderQueryStore>(_ => new OrderQueryStore(
            configuration.GetConnectionString("BookBackendDb")
            ?? throw new InvalidOperationException("ConnectionStrings:BookBackendDb is required.")));

        services.AddScoped<IUnboxingService, UnboxingService>();
        services.AddScoped<IUnboxingQueryStore>(_ => new UnboxingQueryStore(
            configuration.GetConnectionString("BookBackendDb")
            ?? throw new InvalidOperationException("ConnectionStrings:BookBackendDb is required.")));

        services.AddScoped<ICatalogService, CatalogService>();
        services.AddScoped<ICatalogQueryStore>(_ => new CatalogQueryStore(
            configuration.GetConnectionString("BookBackendDb")
            ?? throw new InvalidOperationException("ConnectionStrings:BookBackendDb is required.")));

        services.AddScoped<IFileStorageService, FileStorageService>();
        services.AddScoped<IFileMetadataStore>(_ => new FileMetadataStore(
            configuration.GetConnectionString("BookBackendDb")
            ?? throw new InvalidOperationException("ConnectionStrings:BookBackendDb is required.")));
        services.AddScoped<IFileObjectStore>(_ => new SeaweedFileObjectStore(
            configuration.GetSection("ObjectStorage").Get<ObjectStorageOptions>() ?? new ObjectStorageOptions()));
        services.AddScoped<IUserGalleryService, UserGalleryService>();
        services.AddScoped<IUserGalleryQueryStore>(_ => new UserGalleryQueryStore(
            configuration.GetConnectionString("BookBackendDb")
            ?? throw new InvalidOperationException("ConnectionStrings:BookBackendDb is required.")));

        return services;
    }

    private static void IncludeXmlComments(Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions options, Assembly assembly)
    {
        var xmlFileName = $"{assembly.GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFileName);
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
        }
    }
}