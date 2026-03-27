namespace Runner.Models;

public sealed class TestOutcome
{
    public string Name { get; set; } = string.Empty;

    public bool Passed { get; set; }
}
