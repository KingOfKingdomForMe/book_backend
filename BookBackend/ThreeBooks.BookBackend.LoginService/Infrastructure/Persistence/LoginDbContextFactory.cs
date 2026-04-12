using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ThreeBooks.BookBackend.LoginService.Infrastructure.Persistence;

public sealed class LoginDbContextFactory : IDesignTimeDbContextFactory<LoginDbContext>
{
    public LoginDbContext CreateDbContext(string[] args)
    {
        var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
            .AddJsonFile("appsettings.Local.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("LoginDb")
            ?? throw new InvalidOperationException("ConnectionStrings:LoginDb is required to create LoginDbContext.");

        var optionsBuilder = new DbContextOptionsBuilder<LoginDbContext>();
        optionsBuilder.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36)));

        return new LoginDbContext(optionsBuilder.Options);
    }
}