using System.Text.Json;
using System.Text.RegularExpressions;

namespace Runner.Pipeline;

public sealed class DatabaseSetupService
{
    private static readonly Regex ServerPattern   = new(@"(?:Server|Data Source)\s*=\s*([^;]+)", RegexOptions.IgnoreCase);
    private static readonly Regex UserPattern     = new(@"(?:User Id|UID)\s*=\s*([^;]+)", RegexOptions.IgnoreCase);
    private static readonly Regex PasswordPattern = new(@"(?:Password|PWD)\s*=\s*([^;]+)", RegexOptions.IgnoreCase);
    private static readonly Regex DatabasePattern = new(@"(?:Database|Initial Catalog)\s*=\s*([^;]+)", RegexOptions.IgnoreCase);

    /// <summary>
    /// Finds the first .sql seed script inside database/.
    /// Returns null if folder doesn't exist or has no .sql files.
    /// </summary>
    public static string? FindSeedScript(string databaseRoot)
    {
        if (!Directory.Exists(databaseRoot)) return null;
        return Directory.GetFiles(databaseRoot, "*.sql", SearchOption.TopDirectoryOnly).FirstOrDefault();
    }

    /// <summary>
    /// Reads the runner's SQL Server connection string from database/config.json.
    /// Returns null if the file is missing or malformed.
    /// </summary>
    public static string? ReadRunnerConnectionString(string databaseRoot)
    {
        var configPath = Path.Combine(databaseRoot, "config.json");
        if (!File.Exists(configPath)) return null;
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(configPath));
            if (doc.RootElement.TryGetProperty("connectionString", out var val))
                return val.GetString();
        }
        catch { }
        return null;
    }


    /// <summary>
    /// Drops and recreates the target database, then runs the seed script.
    /// Called once before grading starts to give all submissions a clean, identical dataset.
    /// </summary>
    public async Task SeedAsync(
        string connectionString,
        string seedScriptPath,
        string logPath,
        CancellationToken cancellationToken)
    {
        var (server, user, password) = ParseCredentials(connectionString);
        var dbName = Extract(connectionString, DatabasePattern)?.Trim()
            ?? throw new PipelineException("TEST_RUN_FAILED", "No database name found in database/config.json connection string.");

        var recreateSql =
            $"IF EXISTS(SELECT name FROM sys.databases WHERE name=N'{dbName}') " +
            $"BEGIN ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{dbName}] END; " +
            $"CREATE DATABASE [{dbName}];";

        var createResult = await ProcessRunner.RunAsync(
            "sqlcmd",
            $"-S \"{server}\" -U \"{user}\" -P \"{password}\" -Q \"{recreateSql}\"",
            Path.GetTempPath(),
            logPath,
            cancellationToken: cancellationToken);

        if (createResult.ExitCode != 0)
            throw new PipelineException("TEST_RUN_FAILED", "Failed to recreate database. Check reports/db.log for details.");

        var seedResult = await ProcessRunner.RunAsync(
            "sqlcmd",
            $"-S \"{server}\" -U \"{user}\" -P \"{password}\" -d \"{dbName}\" -i \"{seedScriptPath}\"",
            Path.GetTempPath(),
            logPath,
            cancellationToken: cancellationToken);

        if (seedResult.ExitCode != 0)
            throw new PipelineException("TEST_RUN_FAILED", "Failed to seed database. Check reports/db.log for details.");
    }

    private static (string server, string user, string password) ParseCredentials(string connectionString)
    {
        var server   = Extract(connectionString, ServerPattern)
            ?? throw new PipelineException("TEST_RUN_FAILED", "Cannot parse Server from database/config.json.");
        var user     = Extract(connectionString, UserPattern)
            ?? throw new PipelineException("TEST_RUN_FAILED", "Cannot parse User Id from database/config.json.");
        var password = Extract(connectionString, PasswordPattern)
            ?? throw new PipelineException("TEST_RUN_FAILED", "Cannot parse Password from database/config.json.");

        return (server.Trim(), user.Trim(), password.Trim());
    }

    private static string? Extract(string input, Regex pattern)
    {
        var m = pattern.Match(input);
        return m.Success ? m.Groups[1].Value.Trim() : null;
    }
}
