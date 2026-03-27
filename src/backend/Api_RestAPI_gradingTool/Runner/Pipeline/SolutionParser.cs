using System.Text;
using System.Text.RegularExpressions;

namespace Runner.Pipeline;

public static class SolutionParser
{
    // Matches: Project("{type-guid}") = "Name", "Relative\Path.csproj", "{project-guid}"
    private static readonly Regex ProjectLinePattern = new(
        @"Project\(""\{[^}]+\}""\)\s*=\s*""[^""]+""\s*,\s*""([^""]+\.csproj)""\s*,\s*""(\{[0-9A-Fa-f\-]+\})""",
        RegexOptions.IgnoreCase);

    // Matches StartupProject= followed by optional non-GUID chars then the GUID
    private static readonly Regex StartupProjectPattern = new(
        @"StartupProject[^{]*(\{[0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12}\})",
        RegexOptions.IgnoreCase);

    public static string FindSolutionFile(string directory)
    {
        var files = Directory.GetFiles(directory, "*.sln", SearchOption.AllDirectories);
        if (files.Length == 0)
        {
            throw new PipelineException("BUILD_FAILED", "No .sln file found in submission.");
        }

        return files[0];
    }

    /// <summary>
    /// Returns all (absolutePath, projectGuid) pairs for .csproj files listed in the solution.
    /// </summary>
    public static List<(string Path, string Guid)> ExtractProjects(string solutionPath)
    {
        var solutionDir = Path.GetDirectoryName(solutionPath)!;
        var content = File.ReadAllText(solutionPath);

        return ProjectLinePattern
            .Matches(content)
            .Select(m =>
            {
                var relativePath = m.Groups[1].Value.Replace('\\', Path.DirectorySeparatorChar);
                var absolutePath = Path.GetFullPath(Path.Combine(solutionDir, relativePath));
                var guid = m.Groups[2].Value.ToUpperInvariant();
                return (Path: absolutePath, Guid: guid);
            })
            .Where(p => File.Exists(p.Path))
            .ToList();
    }

    /// <summary>
    /// Reads the .suo binary file (UTF-16) and extracts the StartupProject GUID.
    /// Returns null if .suo is not found or does not contain a StartupProject entry.
    /// </summary>
    public static string? TryReadStartupProjectGuid(string solutionPath)
    {
        var solutionDir = Path.GetDirectoryName(solutionPath)!;

        var suoFile = Directory
            .GetFiles(solutionDir, "*.suo", SearchOption.AllDirectories)
            .FirstOrDefault();

        if (suoFile is null) return null;

        try
        {
            var bytes = File.ReadAllBytes(suoFile);
            var text = Encoding.Unicode.GetString(bytes);
            var match = StartupProjectPattern.Match(text);
            return match.Success ? match.Groups[1].Value.ToUpperInvariant() : null;
        }
        catch
        {
            return null;
        }
    }
}
