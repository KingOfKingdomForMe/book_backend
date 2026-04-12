using ThreeBooks.BookBackend.LoginService.Api.Endpoints;

namespace ThreeBooks.BookBackend.LoginService.Extensions;

public static class EndpointRouteBuilderExtensions
{
    public static WebApplication MapApiEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api");

        api.MapGet("/", () => Results.Ok(new
        {
            service = "ThreeBooks.BookBackend.LoginService",
            status = "running",
            timestampUtc = DateTimeOffset.UtcNow
        })).AllowAnonymous();

        api.MapAuthEndpoints();
        api.MapWeChatEndpoints();
        api.MapUserEndpoints();
        api.MapRoleEndpoints();

        return app;
    }
}