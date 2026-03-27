namespace Runner.Pipeline;

public sealed class ProcessExecutionResult
{
    public int ExitCode { get; init; }

    public string StdOut { get; init; } = string.Empty;

    public string StdErr { get; init; } = string.Empty;

    public string CombinedOutput =>
        string.IsNullOrWhiteSpace(StdErr) ? StdOut : $"{StdOut}{Environment.NewLine}{StdErr}".Trim();
}
