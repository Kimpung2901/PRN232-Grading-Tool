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
