namespace Runner.Models;

public sealed class BatchPipelineOutput
{
    public string Status { get; set; } = "completed";

    public int TotalSubmissions { get; set; }

    public int CompletedSubmissions { get; set; }

    public int FailedSubmissions { get; set; }

    public List<PipelineConsoleOutput> Submissions { get; set; } = [];
}
