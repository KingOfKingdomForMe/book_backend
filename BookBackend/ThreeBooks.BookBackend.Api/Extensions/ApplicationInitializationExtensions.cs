using System.Text;
using MySqlConnector;

namespace ThreeBooks.BookBackend.Api.Extensions;

public static class ApplicationInitializationExtensions
{
    private static readonly string[] ScriptModuleOrder =
    [
        "Common",
        "Files",
        "Catalogs",
        "AlbumTemplates",
        "DefaultAlbums",
        "Albums",
        "Orders",
        "Unboxings"
    ];

    public static async Task InitializeBookBackendApiAsync(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        await ApplyInfrastructureSqlScriptsAsync(app);
    }

    private static async Task ApplyInfrastructureSqlScriptsAsync(WebApplication app)
    {
        if (!app.Configuration.GetValue<bool>("DatabaseInitialization:ApplyInfrastructureSqlScriptsOnStartup"))
        {
            return;
        }

        var logger = app.Services
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("BookBackend.DatabaseInitialization");

        var connectionString = app.Configuration.GetConnectionString("BookBackendDb");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:BookBackendDb is required when DatabaseInitialization:ApplyInfrastructureSqlScriptsOnStartup is enabled.");
        }

        var scriptsRoot = Path.Combine(AppContext.BaseDirectory, "InfrastructureSql");
        if (!Directory.Exists(scriptsRoot))
        {
            throw new DirectoryNotFoundException(
                $"Infrastructure SQL script directory was not found: {scriptsRoot}");
        }

        var scriptPaths = Directory
            .GetFiles(scriptsRoot, "*.sql", SearchOption.AllDirectories)
            .Where(path => IsAutoApplyScript(Path.GetRelativePath(scriptsRoot, path)))
            .OrderBy(path => GetModuleOrder(Path.GetRelativePath(scriptsRoot, path)))
            .ThenBy(path => Path.GetRelativePath(scriptsRoot, path), StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (scriptPaths.Length == 0)
        {
            logger.LogWarning("Infrastructure SQL bootstrap is enabled but no SQL scripts were found under {ScriptsRoot}.", scriptsRoot);
            return;
        }

        logger.LogInformation(
            "Applying {ScriptCount} infrastructure SQL script files from {ScriptsRoot}.",
            scriptPaths.Length,
            scriptsRoot);

        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        foreach (var scriptPath in scriptPaths)
        {
            var relativePath = Path.GetRelativePath(scriptsRoot, scriptPath);
            var scriptContent = await File.ReadAllTextAsync(scriptPath, Encoding.UTF8);
            var statements = ParseStatements(scriptContent);

            logger.LogInformation(
                "Executing infrastructure SQL script {ScriptPath} ({StatementCount} statements).",
                relativePath,
                statements.Count);

            foreach (var statement in statements)
            {
                await using var command = connection.CreateCommand();
                command.CommandText = statement;
                await command.ExecuteNonQueryAsync();
            }
        }

        logger.LogInformation("Infrastructure SQL bootstrap completed successfully.");
    }

    private static int GetModuleOrder(string scriptPath)
    {
        var module = NormalizeRelativePath(scriptPath).Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (string.IsNullOrWhiteSpace(module))
        {
            return int.MaxValue;
        }

        var index = Array.FindIndex(
            ScriptModuleOrder,
            candidate => string.Equals(candidate, module, StringComparison.OrdinalIgnoreCase));

        return index < 0 ? int.MaxValue : index;
    }

    private static bool IsAutoApplyScript(string relativePath)
    {
        var normalizedPath = NormalizeRelativePath(relativePath);

        if (normalizedPath.StartsWith("Common/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (normalizedPath.StartsWith("Files/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (normalizedPath.StartsWith("Catalogs/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (normalizedPath.StartsWith("Orders/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (normalizedPath.StartsWith("AlbumTemplates/Procedures/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (normalizedPath.StartsWith("DefaultAlbums/", StringComparison.OrdinalIgnoreCase)
            && !normalizedPath.Contains("seed", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (normalizedPath.StartsWith("Albums/", StringComparison.OrdinalIgnoreCase)
            && normalizedPath.Contains("procedures", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (normalizedPath.StartsWith("Unboxings/", StringComparison.OrdinalIgnoreCase)
            && normalizedPath.Contains("procedures", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static string NormalizeRelativePath(string relativePath)
    {
        return relativePath.Replace('\\', '/');
    }

    private static IReadOnlyCollection<string> ParseStatements(string content)
    {
        var statements = new List<string>();
        var currentDelimiter = ";";
        var buffer = new StringBuilder();

        using var reader = new StringReader(content);

        while (reader.ReadLine() is { } line)
        {
            var trimmed = line.Trim();

            if (trimmed.StartsWith("DELIMITER ", StringComparison.OrdinalIgnoreCase))
            {
                currentDelimiter = trimmed[10..].Trim();
                continue;
            }

            buffer.AppendLine(line);

            if (trimmed.Length == 0 || !trimmed.EndsWith(currentDelimiter, StringComparison.Ordinal))
            {
                continue;
            }

            var statement = buffer.ToString().Trim();
            statement = statement[..^currentDelimiter.Length].TrimEnd();

            if (!string.IsNullOrWhiteSpace(statement))
            {
                statements.Add(statement);
            }

            buffer.Clear();
        }

        var tail = buffer.ToString().Trim();
        if (!string.IsNullOrWhiteSpace(tail))
        {
            statements.Add(tail);
        }

        return statements;
    }
}