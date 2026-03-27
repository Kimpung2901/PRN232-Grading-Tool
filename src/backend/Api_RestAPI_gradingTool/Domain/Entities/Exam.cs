using System.Collections.Generic;

namespace Infrastructure;

public partial class Exam
{
    public int ExamId { get; set; }

    public string ExamName { get; set; } = null!;

    public string? SqlScriptPath { get; set; }

    public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();

    public virtual ICollection<TestCase> TestCases { get; set; } = new List<TestCase>();
}
