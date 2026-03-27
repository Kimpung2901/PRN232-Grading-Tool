namespace Infrastructure;

public partial class TestResult
{
    public int Id { get; set; }

    public int SubmissionId { get; set; }

    public decimal Score { get; set; }

    public string? ReportPath { get; set; }

    public virtual Submission Submission { get; set; } = null!;
}
