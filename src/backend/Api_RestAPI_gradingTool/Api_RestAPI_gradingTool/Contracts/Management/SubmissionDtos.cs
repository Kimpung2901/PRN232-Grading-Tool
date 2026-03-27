namespace Api_RestAPI_gradingTool.Contracts.Management;

public sealed class SubmissionDto
{
    public int Id { get; set; }
    public int ExamId { get; set; }
    public string StudentName { get; set; } = null!;
    public string StudentCode { get; set; } = null!;
    public string FilePath { get; set; } = null!;
    public int Status { get; set; }
}

public sealed class PatchSubmissionRequest
{
    public string? StudentName { get; set; }
    public string? StudentCode { get; set; }
    public IFormFile? File { get; set; }
}

public sealed class SubmissionUploadRequest
{
    public string StudentName { get; set; } = null!;
    public string StudentCode { get; set; } = null!;
    public IFormFile File { get; set; } = null!;
}

public sealed class SubmissionStatusDto
{
    public int SubmissionId { get; set; }
    public int StatusCode { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? LastError { get; set; }
    public DateTime SubmittedAt { get; set; }
}

public sealed class SubmissionReportDto
{
    public int SubmissionId { get; set; }
    public int ExamId { get; set; }
    public decimal TotalScore { get; set; }
    public string? LastError { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<SubmissionTestResultDto> Results { get; set; } = [];
}

public sealed class SubmissionTestResultDto
{
    public int TestCaseId { get; set; }
    public string TestCaseName { get; set; } = string.Empty;
    public string Outcome { get; set; } = string.Empty;
    public decimal MaxScore { get; set; }
    public decimal EarnedScore { get; set; }
    public string? Log { get; set; }
}
