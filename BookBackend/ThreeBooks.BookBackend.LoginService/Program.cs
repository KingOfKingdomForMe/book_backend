using ThreeBooks.BookBackend.LoginService.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseWindowsService(options =>
{
    options.ServiceName = "ThreeBooks.BookBackend.LoginService";
});

builder.Configuration
    .AddJsonFile("appsettings.Service.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Services.AddLoginService(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();

var swaggerEnabled = app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled");

if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

await app.InitializeLoginServiceAsync();

app.MapControllers();

app.Run();

public partial class Program;
