namespace Runner.Models;

public sealed class TestResultPayload
{
    public int SubmissionId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public int Score { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ReportFilePath { get; set; } = string.Empty;
}
