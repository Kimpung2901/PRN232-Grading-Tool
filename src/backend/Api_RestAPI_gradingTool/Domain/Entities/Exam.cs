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

    public DateTime CreatedAt { get; set; }

    public virtual ExamSession Session { get; set; } = null!;

    public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();

    public virtual ICollection<TestCase> TestCases { get; set; } = new List<TestCase>();
}
