using System.Diagnostics;

namespace Api_RestAPI_gradingTool.Services.Grading;

public static class ProcessRunner
{
    public static async Task<int> RunAsync(
        string fileName,
        string arguments,
        string workingDirectory,
        string logPath,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(logPath) ?? workingDirectory);

        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        await using var logStream = new FileStream(logPath, FileMode.Create, FileAccess.Write, FileShare.Read);
        await using var writer = new StreamWriter(logStream);

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null) writer.WriteLine(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null) writer.WriteLine(e.Data);
        };

        if (!process.Start())
        {
            await writer.WriteLineAsync("Failed to start process.");
            return -1;
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken);
        await writer.FlushAsync();
        return process.ExitCode;
    }
}
