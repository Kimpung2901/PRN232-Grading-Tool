using System;

namespace Api_RestAPI_gradingTool.Contracts.Management;

public sealed class ExamDto
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public string Name { get; set; } = null!;
    public string? DatabaseFilePath { get; set; }
    public string? CollectionFilePath { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class CreateExamRequest
{
    public string Name { get; set; } = null!;
}

public sealed class UpdateExamRequest
{
    public string Name { get; set; } = null!;
}

public sealed class PatchExamRequest
{
    public string? Name { get; set; }
}
