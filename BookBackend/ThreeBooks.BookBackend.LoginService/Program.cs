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

app.MapControllers();

app.Run();

public partial class Program;
