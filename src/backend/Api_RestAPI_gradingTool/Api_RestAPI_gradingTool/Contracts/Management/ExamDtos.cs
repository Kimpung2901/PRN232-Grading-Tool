namespace Api_RestAPI_gradingTool.Contracts.Management;

public sealed class ExamDto
{
    public int ExamId { get; set; }
    public string ExamName { get; set; } = null!;
    public string? SqlScriptPath { get; set; }
}

public sealed class CreateExamRequest
{
    public string ExamName { get; set; } = null!;
    public IFormFile? SqlFile { get; set; }
}

public sealed class PatchExamRequest
{
    public string? ExamName { get; set; }
    public IFormFile? SqlFile { get; set; }
}
