namespace Runner.Models;

public sealed class ResultSummary
{
    public int AssertionsTotal { get; set; }

    public int AssertionsPassed { get; set; }

    public int AssertionsFailed { get; set; }

    public decimal ScorePercent { get; set; }
}
