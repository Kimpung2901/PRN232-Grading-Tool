using System;

namespace Infrastructure;

public partial class RunLog
{
    public int Id { get; set; }

    public int JobId { get; set; }

    public string Type { get; set; } = null!;

    public string? FilePath { get; set; }

    public string? Content { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual GradingJob Job { get; set; } = null!;
}
