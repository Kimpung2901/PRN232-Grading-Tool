using System;
using System.Collections.Generic;

namespace Infrastructure;

public partial class Exam
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

    public virtual ExamSession Session { get; set; } = null!;

    public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();

    public virtual ICollection<TestCase> TestCases { get; set; } = new List<TestCase>();
}
