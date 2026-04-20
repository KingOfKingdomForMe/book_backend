using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using ThreeBooks.BookBackend.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseWindowsService(options =>
{
    options.ServiceName = "ThreeBooks.BookBackend.Api";
});

builder.Configuration
    .AddJsonFile("appsettings.Service.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

if (builder.Environment.IsDevelopment() && !WindowsServiceHelpers.IsWindowsService())
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

public partial class Program;
