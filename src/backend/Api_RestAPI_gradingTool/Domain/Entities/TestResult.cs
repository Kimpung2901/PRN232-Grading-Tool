using System;
using System.Collections.Generic;

namespace Infrastructure;

public partial class TestResult
{
    public int Id { get; set; }

    public int SubmissionId { get; set; }

    public int TestCaseId { get; set; }

    public bool IsPassed { get; set; }

    public bool IsSkipped { get; set; }

    public decimal EarnedScore { get; set; }

    public string? Log { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Submission Submission { get; set; } = null!;

    public virtual TestCase TestCase { get; set; } = null!;
}
