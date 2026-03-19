namespace Runner.Pipeline;

internal static class RunnerPathResolver
{
    public static string ResolveRunnerRoot()
    {
        var candidates = new[]
        {
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory
        };

        foreach (var candidate in candidates)
        {
            var resolved = FindRunnerRoot(candidate);
            if (!string.IsNullOrWhiteSpace(resolved))
            {
                return resolved;
            }
        }

        return Directory.GetCurrentDirectory();
    }

    private static string? FindRunnerRoot(string startPath)
    {
        var directory = new DirectoryInfo(startPath);
        while (directory is not null)
        {
            var runnerProject = Path.Combine(directory.FullName, "Runner.csproj");
            if (File.Exists(runnerProject))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
