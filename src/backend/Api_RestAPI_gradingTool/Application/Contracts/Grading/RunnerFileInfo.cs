namespace Application.Contracts.Grading;

public sealed class RunnerFileInfo
{
    public string RelativePath { get; set; } = null!;
    public string FullPath { get; set; } = null!;
    public string FileName { get; set; } = null!;
}
