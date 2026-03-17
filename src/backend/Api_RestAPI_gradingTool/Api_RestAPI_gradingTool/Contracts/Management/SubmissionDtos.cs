using System;

namespace Api_RestAPI_gradingTool.Contracts.Management;

public sealed class SubmissionDto
{
    public int Id { get; set; }
    public int ExamId { get; set; }
    public string StudentName { get; set; } = null!;
    public string FileName { get; set; } = null!;
    public string FilePath { get; set; } = null!;
    public DateTime SubmittedAt { get; set; }
    public int Status { get; set; }
    public decimal TotalScore { get; set; }
    public string? LastError { get; set; }
}

public sealed class SubmissionUploadRequest
{
    public string StudentName { get; set; } = null!;
    public IFormFile File { get; set; } = null!;
}
