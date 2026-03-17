using System.Diagnostics;
using System.Text;

namespace Runner.Pipeline;

internal static class ProcessRunner
{
    public static async Task<ProcessExecutionResult> RunAsync(
        string fileName,
        string arguments,
        string workingDirectory,
        string logFilePath,
        bool append = false,
        IDictionary<string, string?>? environmentVariables = null,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(logFilePath)!);

        using var process = CreateProcess(fileName, arguments, workingDirectory, environmentVariables);
        var stdOut = new StringBuilder();
        var stdErr = new StringBuilder();

        using var logWriter = new StreamWriter(logFilePath, append, Encoding.UTF8);

        process.OutputDataReceived += (_, args) =>
        {
            if (args.Data is null)
            {
                return;
            }

            lock (stdOut)
            {
                stdOut.AppendLine(args.Data);
            }

            lock (logWriter)
            {
                logWriter.WriteLine(args.Data);
                logWriter.Flush();
            }
        };

        process.ErrorDataReceived += (_, args) =>
        {
            if (args.Data is null)
            {
                return;
            }

            lock (stdErr)
            {
                stdErr.AppendLine(args.Data);
            }

            lock (logWriter)
            {
                logWriter.WriteLine(args.Data);
                logWriter.Flush();
            }
        };

        if (!process.Start())
        {
            throw new InvalidOperationException($"Failed to start process: {fileName} {arguments}");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken);

        return new ProcessExecutionResult
        {
            ExitCode = process.ExitCode,
            StdOut = stdOut.ToString().Trim(),
            StdErr = stdErr.ToString().Trim()
        };
    }

    public static Process StartLongRunningProcess(
        string fileName,
        string arguments,
        string workingDirectory,
        string logFilePath,
        IDictionary<string, string?>? environmentVariables = null)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(logFilePath)!);

        var process = CreateProcess(fileName, arguments, workingDirectory, environmentVariables);
        var logWriter = TextWriter.Synchronized(new StreamWriter(logFilePath, append: false, Encoding.UTF8)
        {
            AutoFlush = true
        });

        process.OutputDataReceived += (_, args) =>
        {
            if (args.Data is not null)
            {
                logWriter.WriteLine(args.Data);
            }
        };

        process.ErrorDataReceived += (_, args) =>
        {
            if (args.Data is not null)
            {
                logWriter.WriteLine(args.Data);
            }
        };

        process.EnableRaisingEvents = true;
        process.Exited += (_, _) => logWriter.Dispose();

        if (!process.Start())
        {
            logWriter.Dispose();
            process.Dispose();
            throw new InvalidOperationException($"Failed to start process: {fileName} {arguments}");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        return process;
    }

    private static Process CreateProcess(
        string fileName,
        string arguments,
        string workingDirectory,
        IDictionary<string, string?>? environmentVariables)
    {
        var resolvedFileName = fileName;
        var resolvedArguments = arguments;

        if (OperatingSystem.IsWindows() && RequiresCommandShell(fileName))
        {
            resolvedFileName = "cmd.exe";
            resolvedArguments = $"/c \"\"{fileName}\" {arguments}\"";
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = resolvedFileName,
            Arguments = resolvedArguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = false,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (environmentVariables is not null)
        {
            foreach (var kvp in environmentVariables)
            {
                startInfo.Environment[kvp.Key] = kvp.Value;
            }
        }

        return new Process
        {
            StartInfo = startInfo
        };
    }

    private static bool RequiresCommandShell(string fileName)
    {
        return fileName.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase) ||
               fileName.EndsWith(".bat", StringComparison.OrdinalIgnoreCase);
    }
}
