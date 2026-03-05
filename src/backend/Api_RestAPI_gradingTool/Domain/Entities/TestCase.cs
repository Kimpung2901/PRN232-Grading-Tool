using System;
using System.Collections.Generic;

namespace Infrastructure;

public partial class TestCase
{
    public int Id { get; set; }

    public int ExamId { get; set; }

    public string Name { get; set; } = null!;

    public string ScriptContent { get; set; } = null!;

    public decimal Score { get; set; }

    public int? DependencyTestCaseId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual TestCase? DependencyTestCase { get; set; }

    public virtual Exam Exam { get; set; } = null!;

    public virtual ICollection<TestCase> InverseDependencyTestCase { get; set; } = new List<TestCase>();

    public virtual ICollection<TestResult> TestResults { get; set; } = new List<TestResult>();
}
