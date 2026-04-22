using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using ThreeBooks.BookBackend.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);
var isWindowsService = WindowsServiceHelpers.IsWindowsService();

builder.Host.UseWindowsService(options =>
{
    options.ServiceName = "ThreeBooks.BookBackend.Api";
});

if (isWindowsService)
{
    builder.Configuration.AddJsonFile("appsettings.Service.json", optional: true, reloadOnChange: true);
}

builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

if (builder.Environment.IsDevelopment()
    && !isWindowsService
    && !HasExplicitDebugUrls(builder.Configuration))
{
    builder.WebHost.UseUrls("http://localhost:5281");
}

builder.Services.AddBookBackendApi(builder.Configuration);

var app = builder.Build();

var swaggerEnabled = app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled");

if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();

static bool HasExplicitDebugUrls(IConfiguration configuration)
{
    return !string.IsNullOrWhiteSpace(configuration["ASPNETCORE_URLS"])
        || !string.IsNullOrWhiteSpace(configuration["DOTNET_URLS"])
        || !string.IsNullOrWhiteSpace(configuration["URLS"])
        || !string.IsNullOrWhiteSpace(configuration["Urls"]);
}

public partial class Program;
