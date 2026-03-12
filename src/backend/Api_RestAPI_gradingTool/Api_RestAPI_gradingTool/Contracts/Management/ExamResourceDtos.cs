using System;

namespace Api_RestAPI_gradingTool.Contracts.Management;

public sealed class ExamResourceDto
{
    public int ExamId { get; set; }
    public string? DatabaseFilePath { get; set; }
    public string CollectionFilePath { get; set; } = null!;
    public DateTime UpdatedAt { get; set; }
}

public sealed class ExamResourceUploadRequest
{
    public IFormFile Collection { get; set; } = null!;
    public IFormFile? Database { get; set; }
}

public sealed class ExamResourceListItemDto
{
    public int ExamId { get; set; }
    public string ExamName { get; set; } = null!;
    public string? DatabaseFilePath { get; set; }
    public string CollectionFilePath { get; set; } = null!;
    public DateTime ExamCreatedAt { get; set; }
}
