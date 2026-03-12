using System;

namespace Api_RestAPI_gradingTool.Contracts.Management;

public sealed class ExamResourceDto
{
    public int ExamId { get; set; }
    public string? DatabaseFilePath { get; set; }
    public string CollectionFilePath { get; set; } = null!;
    public DateTime UpdatedAt { get; set; }
}
