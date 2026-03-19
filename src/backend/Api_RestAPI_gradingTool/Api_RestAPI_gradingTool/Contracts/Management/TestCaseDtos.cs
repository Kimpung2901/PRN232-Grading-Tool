using System;

namespace Api_RestAPI_gradingTool.Contracts.Management;

public sealed class TestCaseDto
{
    public int Id { get; set; }
    public int ExamId { get; set; }
    public string Name { get; set; } = null!;
    public string PostmanItemId { get; set; } = null!;
    public decimal Score { get; set; }
    public int? DependencyTestCaseId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class CreateTestCaseRequest
{
    public string Name { get; set; } = null!;
    public string PostmanItemId { get; set; } = null!;
    public decimal Score { get; set; }
    public int? DependencyTestCaseId { get; set; }
}

public sealed class UpdateTestCaseRequest
{
    public string Name { get; set; } = null!;
    public string PostmanItemId { get; set; } = null!;
    public decimal Score { get; set; }
    public int? DependencyTestCaseId { get; set; }
}

public sealed class PatchTestCaseRequest
{
    public string? Name { get; set; }
    public string? PostmanItemId { get; set; }
    public decimal? Score { get; set; }
    public int? DependencyTestCaseId { get; set; }
}

public sealed class ImportTestCaseResultDto
{
    public int ExamId { get; set; }
    public int Created { get; set; }
    public int Skipped { get; set; }
}

public sealed class PagedResult<T>
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public T[] Items { get; set; } = Array.Empty<T>();
}
