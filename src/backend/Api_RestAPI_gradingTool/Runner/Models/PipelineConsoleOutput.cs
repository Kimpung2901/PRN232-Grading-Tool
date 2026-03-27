namespace Runner.Models;

public sealed class PipelineConsoleOutput
{
    public string SubmissionName { get; set; } = string.Empty;

    public string SubmissionFilePath { get; set; } = string.Empty;

    public string Status { get; set; } = "failed";

    public string? Error { get; set; }

    public string? ErrorMessage { get; set; }

    public bool BuildSucceeded { get; set; }

    public bool ApiStarted { get; set; }

    public bool TestRunCompleted { get; set; }

    public bool HasFailedAssertions { get; set; }

    public ResultSummary Summary { get; set; } = new();

    public List<TestOutcome> Results { get; set; } = [];

    public string BuildLog { get; set; } = string.Empty;

    public string RunLog { get; set; } = string.Empty;

    public string NewmanLog { get; set; } = string.Empty;

    public string BuildLogPath { get; set; } = string.Empty;

    public string RunLogPath { get; set; } = string.Empty;

    public string NewmanLogPath { get; set; } = string.Empty;

    public string ReportPath { get; set; } = string.Empty;

    public string ResultPath { get; set; } = string.Empty;

    public double ElapsedSeconds { get; set; }
}
