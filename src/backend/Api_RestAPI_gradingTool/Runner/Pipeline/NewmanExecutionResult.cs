namespace Runner.Pipeline;

public sealed class NewmanExecutionResult
{
    public int ExitCode { get; init; }

    public string LogContent { get; init; } = string.Empty;
}
