namespace Runner.Models;

public sealed class PipelineConsoleOutput
{
    public string? Error { get; set; }

    public string? ErrorMessage { get; set; }

    public List<TestOutcome> Results { get; set; } = [];

    public string BuildLog { get; set; } = string.Empty;

    public string RunLog { get; set; } = string.Empty;
}
