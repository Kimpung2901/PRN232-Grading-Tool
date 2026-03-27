namespace Api_RestAPI_gradingTool.Contracts.Management;

public sealed class TestCaseDto
{
    public int Id { get; set; }
    public int ExamId { get; set; }
    public string FilePath { get; set; } = null!;
}

public sealed class PatchTestCaseRequest
{
    public string? FilePath { get; set; }
    public IFormFile? File { get; set; }
}

public sealed class TestCaseCollectionUploadRequest
{
    public IFormFile File { get; set; } = null!;
}

public sealed class PagedResult<T>
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public T[] Items { get; set; } = Array.Empty<T>();
}
