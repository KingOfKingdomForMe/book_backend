using ThreeBooks.BookBackend.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBookBackendApi(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;
