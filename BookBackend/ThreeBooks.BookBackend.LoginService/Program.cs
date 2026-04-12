using ThreeBooks.BookBackend.LoginService.Api.Endpoints;
using ThreeBooks.BookBackend.LoginService.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Services.AddLoginService(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

await app.InitializeLoginServiceAsync();

app.MapGet("/", () => Results.Ok(new
{
    service = "ThreeBooks.BookBackend.LoginService",
    status = "running",
    timestampUtc = DateTimeOffset.UtcNow
})).AllowAnonymous();

app.MapAuthEndpoints();
app.MapWeChatEndpoints();
app.MapUserEndpoints();
app.MapRoleEndpoints();

app.Run();

public partial class Program;
