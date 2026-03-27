using System.Text.Json;
using Runner.Models;

namespace Runner.Pipeline;

public sealed class ReportParser
{
    public async Task<List<TestOutcome>> ParseAsync(string reportPath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(reportPath))
        {
            throw new FileNotFoundException("Newman report file was not found.", reportPath);
        }

        await using var reportStream = File.OpenRead(reportPath);
        using var document = await JsonDocument.ParseAsync(reportStream, cancellationToken: cancellationToken);

        var outcomes = new List<TestOutcome>();

        if (!document.RootElement.TryGetProperty("run", out var runElement) ||
            !runElement.TryGetProperty("executions", out var executionsElement) ||
            executionsElement.ValueKind != JsonValueKind.Array)
        {
            return outcomes;
        }

        foreach (var execution in executionsElement.EnumerateArray())
        {
            if (!execution.TryGetProperty("assertions", out var assertionsElement) ||
                assertionsElement.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var assertion in assertionsElement.EnumerateArray())
            {
                var name = assertion.TryGetProperty("assertion", out var nameElement)
                    ? nameElement.GetString()
                    : null;

                var passed = !assertion.TryGetProperty("error", out var errorElement) ||
                             errorElement.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined;

                outcomes.Add(new TestOutcome
                {
                    Name = string.IsNullOrWhiteSpace(name) ? "Unnamed assertion" : name,
                    Passed = passed
                });
            }
        }

        return outcomes;
    }
}
