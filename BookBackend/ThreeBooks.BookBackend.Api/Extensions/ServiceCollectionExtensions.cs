using ThreeBooks.BookBackend.Application.Modules.AlbumTemplates.Interfaces;
using ThreeBooks.BookBackend.Application.Modules.AlbumTemplates.Services;
using ThreeBooks.BookBackend.Application.Modules.Catalogs.Interfaces;
using ThreeBooks.BookBackend.Application.Modules.Catalogs.Services;
using ThreeBooks.BookBackend.Application.Modules.Files.Interfaces;
using ThreeBooks.BookBackend.Application.Modules.Files.Services;
using ThreeBooks.BookBackend.Infrastructure.Persistence.Files;
using ThreeBooks.BookBackend.Infrastructure.Persistence.Queries.AlbumTemplates;
using ThreeBooks.BookBackend.Infrastructure.Persistence.Queries.Catalogs;
using ThreeBooks.BookBackend.Infrastructure.Storage;
using ThreeBooks.BookBackend.Infrastructure.Storage.Options;

namespace ThreeBooks.BookBackend.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBookBackendApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        services.AddScoped<IAlbumTemplateService, AlbumTemplateService>();
        services.AddScoped<IAlbumTemplateQueryStore, AlbumTemplateQueryStore>();

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

        return services;
    }
}