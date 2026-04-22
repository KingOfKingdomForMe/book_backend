using System.Text;
using System.Text.Json;
using MySqlConnector;

var options = ParseArguments(args);

if (string.IsNullOrWhiteSpace(options.ConnectionString))
{
    Console.Error.WriteLine("Missing --connection.");
    return 2;
}

await using var connection = new MySqlConnection(options.ConnectionString);
await connection.OpenAsync();

if (!string.IsNullOrWhiteSpace(options.Query))
{
    await ExecuteQueryAsync(connection, options.Query);
    return 0;
}

if (options.ScriptPaths.Count == 0)
{
    Console.Error.WriteLine("Provide --query or at least one --script.");
    return 2;
}

foreach (var scriptPath in options.ScriptPaths)
{
    await ExecuteScriptFileAsync(connection, scriptPath);
}

return 0;

static RunnerOptions ParseArguments(string[] args)
{
    var options = new RunnerOptions();

    for (var index = 0; index < args.Length; index++)
    {
        switch (args[index])
        {
            case "--connection":
                options.ConnectionString = GetNextValue(args, ref index, "--connection");
                break;
            case "--query":
                options.Query = GetNextValue(args, ref index, "--query");
                break;
            case "--script":
                options.ScriptPaths.Add(GetNextValue(args, ref index, "--script"));
                break;
            default:
                throw new ArgumentException($"Unknown argument: {args[index]}");
        }
    }

    return options;
}

static string GetNextValue(string[] args, ref int index, string optionName)
{
    if (index + 1 >= args.Length)
    {
        throw new ArgumentException($"Missing value for {optionName}.");
    }

    index++;
    return args[index];
}

static async Task ExecuteQueryAsync(MySqlConnection connection, string query)
{
    await using var command = connection.CreateCommand();
    command.CommandText = query;

    await using var reader = await command.ExecuteReaderAsync();
    var rows = new List<Dictionary<string, object?>>();

    while (await reader.ReadAsync())
    {
        var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        for (var ordinal = 0; ordinal < reader.FieldCount; ordinal++)
        {
            row[reader.GetName(ordinal)] = await reader.IsDBNullAsync(ordinal)
                ? null
                : reader.GetValue(ordinal);
        }

        rows.Add(row);
    }

    Console.WriteLine(JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true }));
}

static async Task ExecuteScriptFileAsync(MySqlConnection connection, string scriptPath)
{
    if (!File.Exists(scriptPath))
    {
        throw new FileNotFoundException($"Script file not found: {scriptPath}");
    }

    Console.WriteLine($"Executing script: {scriptPath}");

    var content = await File.ReadAllTextAsync(scriptPath, Encoding.UTF8);
    var statements = ParseStatements(content);

    var executedCount = 0;

    foreach (var statement in statements)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = statement;
        await command.ExecuteNonQueryAsync();
        executedCount++;
    }

    Console.WriteLine($"Executed {executedCount} statement(s) from {Path.GetFileName(scriptPath)}");
}

static IReadOnlyCollection<string> ParseStatements(string content)
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

        if (trimmed.Length == 0)
        {
            continue;
        }

        if (!trimmed.EndsWith(currentDelimiter, StringComparison.Ordinal))
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

sealed class RunnerOptions
{
    public string? ConnectionString { get; set; }

    public string? Query { get; set; }

    public List<string> ScriptPaths { get; } = [];
}