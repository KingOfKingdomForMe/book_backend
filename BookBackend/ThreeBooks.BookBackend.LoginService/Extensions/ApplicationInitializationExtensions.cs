using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ThreeBooks.BookBackend.LoginService.Infrastructure.Persistence;
using ThreeBooks.BookBackend.LoginService.Infrastructure.Seeding;
using ThreeBooks.BookBackend.LoginService.Options;

namespace ThreeBooks.BookBackend.LoginService.Extensions;

public static class ApplicationInitializationExtensions
{
    public static async Task InitializeLoginServiceAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbOptions = scope.ServiceProvider.GetRequiredService<IOptions<LoginDbOptions>>().Value;
        if (!dbOptions.ApplyMigrationsOnStartup)
        {
            return;
        }

        var dbContext = scope.ServiceProvider.GetRequiredService<LoginDbContext>();
        await dbContext.Database.MigrateAsync();

        var seeder = scope.ServiceProvider.GetRequiredService<LoginDbSeeder>();
        await seeder.SeedAsync();
    }
}