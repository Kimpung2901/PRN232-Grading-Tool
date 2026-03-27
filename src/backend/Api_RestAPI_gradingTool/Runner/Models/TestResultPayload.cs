namespace Runner.Models;

public sealed class TestResultPayload
{
    public string StudentName { get; set; } = string.Empty;
    public string Score { get; set; } = "0";
    public string Status { get; set; } = string.Empty;
    public string ReportFilePath { get; set; } = string.Empty;
}
