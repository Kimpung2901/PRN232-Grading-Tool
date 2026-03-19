using System.Text.Json;

namespace Api_RestAPI_gradingTool.Services.Grading;

public sealed class NewmanExecutionResult
{
    public string ItemId { get; init; } = string.Empty;
    public bool Passed { get; init; }
    public string? Log { get; init; }
}

public static class NewmanReportParser
{
    public static IReadOnlyList<NewmanExecutionResult> ParseExecutions(string reportPath)
    {
        using var stream = File.OpenRead(reportPath);
        using var doc = JsonDocument.Parse(stream);

        if (!doc.RootElement.TryGetProperty("run", out var run))
        {
            return Array.Empty<NewmanExecutionResult>();
        }

        if (!run.TryGetProperty("executions", out var executions))
        {
            return Array.Empty<NewmanExecutionResult>();
        }

        var results = new List<NewmanExecutionResult>();
        foreach (var exec in executions.EnumerateArray())
        {
            var itemId = string.Empty;
            if (exec.TryGetProperty("item", out var item))
            {
                if (item.TryGetProperty("id", out var idProp))
                {
                    itemId = idProp.GetString() ?? string.Empty;
                }
                if (string.IsNullOrWhiteSpace(itemId) && item.TryGetProperty("name", out var nameProp))
                {
                    itemId = nameProp.GetString() ?? string.Empty;
                }
            }
            var passed = true;
            var log = string.Empty;
            if (exec.TryGetProperty("assertions", out var assertions) && assertions.ValueKind == JsonValueKind.Array)
            {
                foreach (var assertion in assertions.EnumerateArray())
                {
                    if (assertion.TryGetProperty("error", out var error))
                    {
                        if (error.ValueKind != JsonValueKind.Null && error.ValueKind != JsonValueKind.Undefined)
                        {
                            passed = false;
                            var message = error.TryGetProperty("message", out var msg) ? msg.GetString() : "assertion failed";
                            log += message + "\n";
                        }
                    }
                }
            }

            results.Add(new NewmanExecutionResult
            {
                ItemId = itemId,
                Passed = passed,
                Log = string.IsNullOrWhiteSpace(log) ? null : log.Trim()
            });
        }

        return results;
    }
}
