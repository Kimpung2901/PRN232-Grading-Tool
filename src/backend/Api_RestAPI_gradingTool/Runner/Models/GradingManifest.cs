namespace Runner.Models;

public sealed class GradingManifest
{
    public string CollectionPath { get; set; } = string.Empty;
    public string? SeedScriptPath { get; set; }
    public List<GradingManifestSubmission> Submissions { get; set; } = [];
}

public sealed class GradingManifestSubmission
{
    public int SubmissionId { get; set; }
    public string SubmissionName { get; set; } = string.Empty;
    public string SubmissionFilePath { get; set; } = string.Empty;
}
