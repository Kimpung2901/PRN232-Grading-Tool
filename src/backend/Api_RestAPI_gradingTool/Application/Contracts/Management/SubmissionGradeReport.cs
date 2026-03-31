namespace Application.Contracts.Management;

public sealed class SubmissionGradeReport
{
    public int SubmissionId { get; set; }
    public int ExamId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentCode { get; set; } = string.Empty;
    public decimal TotalScore { get; set; }
    public string? LastError { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ReportPath { get; set; }
}
