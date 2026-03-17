using System;

namespace Api_RestAPI_gradingTool.Contracts.Management;

public sealed class ExamSessionDto
{
    public int Id { get; set; }
    public int SemesterId { get; set; }
    public string Name { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

public sealed class CreateExamSessionRequest
{
    public string Name { get; set; } = null!;
}

public sealed class UpdateExamSessionRequest
{
    public string Name { get; set; } = null!;
}

public sealed class PatchExamSessionRequest
{
    public string? Name { get; set; }
}
