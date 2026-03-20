using Runner.Models;

namespace Runner.Pipeline;

internal static class ResultSummaryFactory
{
    public static ResultSummary Create(IReadOnlyCollection<TestOutcome> outcomes)
    {
        var total = outcomes.Count;
        var passed = outcomes.Count(x => x.Passed);
        var failed = total - passed;
        var scorePercent = total == 0
            ? 0m
            : Math.Round((decimal)passed * 100m / total, 2, MidpointRounding.AwayFromZero);

        return new ResultSummary
        {
            AssertionsTotal = total,
            AssertionsPassed = passed,
            AssertionsFailed = failed,
            ScorePercent = scorePercent
        };
    }
}
