using System.Collections.Generic;

namespace Infrastructure;

public partial class Submission
{
    public int Id { get; set; }

    public int ExamId { get; set; }

    public string StudentName { get; set; } = null!;

    public string StudentCode { get; set; } = null!;

    public string FilePath { get; set; } = null!;

    public int Status { get; set; }

    public virtual Exam Exam { get; set; } = null!;

    public virtual ICollection<TestResult> TestResults { get; set; } = new List<TestResult>();
}
