using System;
using System.Collections.Generic;

namespace Infrastructure;

public partial class ExamSession
{
    public int Id { get; set; }

    public int SemesterId { get; set; }

    public string Name { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Exam> Exams { get; set; } = new List<Exam>();

    public virtual Semester Semester { get; set; } = null!;
}
