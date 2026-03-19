using System;
using System.Collections.Generic;

namespace Infrastructure;

public partial class GradingJob
{
    public int Id { get; set; }

    public int SubmissionId { get; set; }

    public int Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }

    public string? LastError { get; set; }

    public virtual Submission Submission { get; set; } = null!;

    public virtual ICollection<RunLog> RunLogs { get; set; } = new List<RunLog>();
}
