namespace Runner.Pipeline;

public sealed class EnvironmentSetupService
{
    public async Task<string> EnsureNewmanInstalledAsync(string logFilePath, CancellationToken cancellationToken = default)
    {
        var newmanCommand = await ResolveNewmanCommandAsync(logFilePath, cancellationToken);
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

        var npmCommand = ResolveNpmCommand();
        if (!await CommandExistsAsync(npmCommand, "--version", logFilePath, cancellationToken))
        {
            throw new PipelineException("TEST_RUN_FAILED", "npm is not installed, so Newman CLI cannot be installed automatically.");
        }

        var installResult = await ProcessRunner.RunAsync(
            npmCommand,
            "install -g newman",
            Directory.GetCurrentDirectory(),
            logFilePath,
            append: true,
            cancellationToken: cancellationToken);

        if (installResult.ExitCode != 0)
        {
            throw new PipelineException("TEST_RUN_FAILED", $"Automatic Newman CLI installation failed. {installResult.CombinedOutput}");
        }

        newmanCommand = await ResolveNewmanCommandAsync(logFilePath, cancellationToken);
        if (string.IsNullOrWhiteSpace(newmanCommand))
        {
            throw new PipelineException("TEST_RUN_FAILED", "Newman CLI installation completed but the command is still unavailable.");
        }

        return newmanCommand;
    }

    private static async Task<string?> ResolveNewmanCommandAsync(string logFilePath, CancellationToken cancellationToken)
    {
        var directCommand = ResolveNewmanCommandName();
        if (await CommandExistsAsync(directCommand, "--version", logFilePath, cancellationToken))
        {
            return directCommand;
        }

        var npmCommand = ResolveNpmCommand();
        if (!await CommandExistsAsync(npmCommand, "--version", logFilePath, cancellationToken))
        {
            return null;
        }

        var prefixResult = await ProcessRunner.RunAsync(
            npmCommand,
            "prefix -g",
            Directory.GetCurrentDirectory(),
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

    private static string ResolveNpmCommand()
    {
        return OperatingSystem.IsWindows() ? "npm.cmd" : "npm";
    }

    private static string ResolveNewmanCommandName()
    {
        return OperatingSystem.IsWindows() ? "newman.cmd" : "newman";
    }

    private static async Task<bool> CommandExistsAsync(
        string fileName,
        string arguments,
        string logFilePath,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await ProcessRunner.RunAsync(
                fileName,
                arguments,
                Directory.GetCurrentDirectory(),
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
}
