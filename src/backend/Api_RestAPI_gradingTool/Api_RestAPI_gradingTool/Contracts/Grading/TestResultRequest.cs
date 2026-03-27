using System.Text.Json.Serialization;

namespace Api_RestAPI_gradingTool.Contracts.Grading;

public sealed class TestResultRequest
{
    [JsonPropertyName("studentName")]
    public string StudentName { get; set; } = string.Empty;

    [JsonPropertyName("score")]
    public int Score { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("reportFilePath")]
    public string ReportFilePath { get; set; } = string.Empty;
}
