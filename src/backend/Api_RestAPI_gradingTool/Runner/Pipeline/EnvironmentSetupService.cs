namespace Runner.Pipeline;

public sealed class EnvironmentSetupService
{
    public async Task<string> EnsureNewmanInstalledAsync(
        string runnerRoot,
        string logFilePath,
        CancellationToken cancellationToken = default)
    {
        var newmanCommand = await ResolveNewmanCommandAsync(runnerRoot, logFilePath, cancellationToken);
        if (!string.IsNullOrWhiteSpace(newmanCommand))
        {
            return newmanCommand;
        }

        await Console.Out.WriteLineAsync("Newman CLI is not installed.");
        await Console.Out.WriteAsync("Do you want to install Newman now? (y/n): ");

        var answer = await Console.In.ReadLineAsync(cancellationToken);
        if (!IsYes(answer))
        {
            throw new PipelineException("TEST_RUN_FAILED", "Newman CLI is required but was not installed.");
        }

        var npmCommand = await ResolveCommandPathAsync(ResolveNpmCommandName(), runnerRoot, logFilePath, cancellationToken);
        if (string.IsNullOrWhiteSpace(npmCommand) ||
            !await CommandExistsAsync(npmCommand, "--version", runnerRoot, logFilePath, cancellationToken))
        {
            throw new PipelineException("TEST_RUN_FAILED", "npm is not installed, so Newman CLI cannot be installed automatically.");
        }

        var installResult = await ProcessRunner.RunAsync(
            npmCommand,
            $"install newman --prefix \"{GetToolRoot(runnerRoot)}\" --no-audit --no-fund",
            runnerRoot,
            logFilePath,
            append: true,
            environmentVariables: CreateNpmEnvironmentVariables(runnerRoot),
            cancellationToken: cancellationToken);

        if (installResult.ExitCode != 0)
        {
            throw new PipelineException("TEST_RUN_FAILED", $"Automatic Newman CLI installation failed. {installResult.CombinedOutput}");
        }

        newmanCommand = await ResolveNewmanCommandAsync(runnerRoot, logFilePath, cancellationToken);
        if (string.IsNullOrWhiteSpace(newmanCommand))
        {
            throw new PipelineException("TEST_RUN_FAILED", "Newman CLI installation completed but the command is still unavailable.");
        }

        return newmanCommand;
    }

    private static async Task<string?> ResolveNewmanCommandAsync(
        string runnerRoot,
        string logFilePath,
        CancellationToken cancellationToken)
    {
        var localCommand = GetLocalNewmanCommandPath(runnerRoot);
        if (File.Exists(localCommand) &&
            await CommandExistsAsync(localCommand, "--version", runnerRoot, logFilePath, cancellationToken))
        {
            return localCommand;
        }

        var directCommand = await ResolveCommandPathAsync(ResolveNewmanCommandName(), runnerRoot, logFilePath, cancellationToken);
        if (!string.IsNullOrWhiteSpace(directCommand) &&
            await CommandExistsAsync(directCommand, "--version", runnerRoot, logFilePath, cancellationToken))
        {
            return directCommand;
        }

        var npmCommand = await ResolveCommandPathAsync(ResolveNpmCommandName(), runnerRoot, logFilePath, cancellationToken);
        if (string.IsNullOrWhiteSpace(npmCommand) ||
            !await CommandExistsAsync(npmCommand, "--version", runnerRoot, logFilePath, cancellationToken))
        {
            return null;
        }

        var prefixResult = await ProcessRunner.RunAsync(
            npmCommand,
            "prefix -g",
            runnerRoot,
            logFilePath,
            append: true,
            cancellationToken: cancellationToken);

        if (prefixResult.ExitCode != 0)
        {
            return null;
        }

        var prefix = prefixResult.StdOut
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .LastOrDefault();

        if (string.IsNullOrWhiteSpace(prefix))
        {
            return null;
        }

        foreach (var candidatePath in GetNewmanCandidatePaths(prefix))
        {
            if (File.Exists(candidatePath))
            {
                return candidatePath;
            }
        }

        return null;
    }

    private static IEnumerable<string> GetNewmanCandidatePaths(string npmPrefix)
    {
        if (OperatingSystem.IsWindows())
        {
            yield return Path.Combine(npmPrefix, "newman.cmd");

            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (!string.IsNullOrWhiteSpace(appData))
            {
                yield return Path.Combine(appData, "npm", "newman.cmd");
            }

            yield break;
        }

        yield return Path.Combine(npmPrefix, "bin", "newman");
    }

    private static string ResolveNpmCommandName()
    {
        return OperatingSystem.IsWindows() ? "npm.cmd" : "npm";
    }

    private static string ResolveNewmanCommandName()
    {
        return OperatingSystem.IsWindows() ? "newman.cmd" : "newman";
    }

    private static async Task<string?> ResolveCommandPathAsync(
        string commandName,
        string workingDirectory,
        string logFilePath,
        CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsWindows())
        {
            return commandName;
        }

        try
        {
            var result = await ProcessRunner.RunAsync(
                "where.exe",
                commandName,
                workingDirectory,
                logFilePath,
                append: true,
                cancellationToken: cancellationToken);

            if (result.ExitCode != 0)
            {
                return null;
            }

            return result.StdOut
                .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    private static async Task<bool> CommandExistsAsync(
        string fileName,
        string arguments,
        string workingDirectory,
        string logFilePath,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await ProcessRunner.RunAsync(
                fileName,
                arguments,
                workingDirectory,
                logFilePath,
                append: true,
                cancellationToken: cancellationToken);

            return result.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsYes(string? answer)
    {
        if (string.IsNullOrWhiteSpace(answer))
        {
            return false;
        }

        var normalized = answer.Trim();
        return normalized.Equals("y", StringComparison.OrdinalIgnoreCase) ||
               normalized.Equals("yes", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetToolRoot(string runnerRoot)
    {
        return Path.Combine(runnerRoot, ".tools", "newman");
    }

    private static Dictionary<string, string?> CreateNpmEnvironmentVariables(string runnerRoot)
    {
        var npmHome = Path.Combine(runnerRoot, ".tools", "npm-home");
        var npmCache = Path.Combine(runnerRoot, ".tools", "npm-cache");

        Directory.CreateDirectory(npmHome);
        Directory.CreateDirectory(npmCache);

        return new Dictionary<string, string?>
        {
            ["npm_config_cache"] = npmCache,
            ["npm_config_userconfig"] = Path.Combine(npmHome, ".npmrc"),
            ["npm_config_globalconfig"] = Path.Combine(npmHome, "global-npmrc")
        };
    }

    private static string GetLocalNewmanCommandPath(string runnerRoot)
    {
        var toolRoot = GetToolRoot(runnerRoot);
        return OperatingSystem.IsWindows()
            ? Path.Combine(toolRoot, "node_modules", ".bin", "newman.cmd")
            : Path.Combine(toolRoot, "node_modules", ".bin", "newman");
    }
}
