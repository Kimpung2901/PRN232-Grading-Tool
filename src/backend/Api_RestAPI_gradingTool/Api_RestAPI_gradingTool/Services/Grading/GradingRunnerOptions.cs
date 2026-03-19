namespace Api_RestAPI_gradingTool.Services.Grading;

public sealed class GradingRunnerOptions
{
    public string WorkingRoot { get; set; } = "data";
    public string DotnetPath { get; set; } = "dotnet";
    public string NewmanPath { get; set; } = "newman";
    public int HealthCheckSeconds { get; set; } = 30;
    public string HealthPath { get; set; } = "/";
    public int RestoreTimeoutSeconds { get; set; } = 300;
    public int BuildTimeoutSeconds { get; set; } = 600;
    public int NewmanTimeoutSeconds { get; set; } = 300;
    public int RunTimeoutSeconds { get; set; } = 900;
    public bool EnableSpecCheck { get; set; } = true;
    public string SwaggerPath { get; set; } = "/swagger/v1/swagger.json";
    public string? SqlCmdPath { get; set; } = "sqlcmd";
    public string? SqlCmdServer { get; set; }
    public string? SqlCmdUser { get; set; }
    public string? SqlCmdPassword { get; set; }
    public string? StudentDbConnection { get; set; }
}
