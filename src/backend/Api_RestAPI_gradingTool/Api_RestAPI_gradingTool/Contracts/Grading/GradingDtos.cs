namespace Api_RestAPI_gradingTool.Contracts.Grading;

public sealed class GradingJobDto
{
    public int Id { get; set; }
    public int SubmissionId { get; set; }
    public int Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public string? LastError { get; set; }
}

public sealed class RunLogDto
{
    public int Id { get; set; }
    public int JobId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? FilePath { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class SubmissionResultDto
{
    public int SubmissionId { get; set; }
    public int Status { get; set; }
    public decimal TotalScore { get; set; }
    public string? LastError { get; set; }
    public TestResultItemDto[] Results { get; set; } = Array.Empty<TestResultItemDto>();
}

public sealed class TestResultItemDto
{
    public int TestCaseId { get; set; }
    public string TestCaseName { get; set; } = string.Empty;
    public bool IsPassed { get; set; }
    public bool IsSkipped { get; set; }
    public decimal EarnedScore { get; set; }
    public string? Log { get; set; }
}
