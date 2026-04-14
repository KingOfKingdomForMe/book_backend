using ThreeBooks.BookBackend.Application.Modules.AlbumTemplates.Interfaces;
using ThreeBooks.BookBackend.Application.Modules.AlbumTemplates.Services;
using ThreeBooks.BookBackend.Infrastructure.Persistence.Queries.AlbumTemplates;

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

        return services;
    }
}