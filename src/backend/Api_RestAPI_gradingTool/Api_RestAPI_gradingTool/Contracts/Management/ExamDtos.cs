using System;

namespace Api_RestAPI_gradingTool.Contracts.Management;

public sealed class ExamDto
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public string Name { get; set; } = null!;
    public string? DatabaseFilePath { get; set; }
    public string? CollectionFilePath { get; set; }
    public string? EndpointSpecFilePath { get; set; }
    public string? EnvironmentFilePath { get; set; }
    public string? StudentDbConnection { get; set; }
    public string? HealthPath { get; set; }
    public string? SwaggerPath { get; set; }
    public string? SqlCmdServer { get; set; }
    public string? SqlCmdUser { get; set; }
    public string? SqlCmdPassword { get; set; }
    public string? NewmanExtraArgs { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? TestCasesCount { get; set; }
    public int? SubmissionsCount { get; set; }
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
    public string? StudentDbConnection { get; set; }
    public string? HealthPath { get; set; }
    public string? SwaggerPath { get; set; }
    public string? SqlCmdServer { get; set; }
    public string? SqlCmdUser { get; set; }
    public string? SqlCmdPassword { get; set; }
    public string? NewmanExtraArgs { get; set; }
}
