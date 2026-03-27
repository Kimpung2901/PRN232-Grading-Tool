using Api_RestAPI_gradingTool.Contracts.Grading;
using Api_RestAPI_gradingTool.Hubs;
using Application.Contracts.Grading;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;

namespace Api_RestAPI_gradingTool.Controllers.Grading;

[Route("api")]
public sealed class TestResultsController : ApiControllerBase
{
    private static readonly object RunnerStateLock = new();
    private static bool _isRunnerRunning;
    private static int? _currentProcessId;
    private static DateTimeOffset? _startedAtUtc;
    private static DateTimeOffset? _lastCompletedAtUtc;
    private static int? _lastExitCode;
    private static string? _lastError;

    private readonly IGradingDbContext _db;
    private readonly IHubContext<TestHub> _hubContext;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<TestResultsController> _logger;

    public TestResultsController(
        IGradingDbContext db,
        IHubContext<TestHub> hubContext,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        ILogger<TestResultsController> logger)
    {
        _db = db;
        _hubContext = hubContext;
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    [HttpGet("testrunner")]
    public ActionResult StartRunner()
    {
        lock (RunnerStateLock)
        {
            if (_isRunnerRunning)
            {
                return Conflict(new
                {
                    message = "Runner is already running.",
                    isRunning = true,
                    processId = _currentProcessId,
                    startedAtUtc = _startedAtUtc
                });
            }
        }

        var runnerExecutablePath = ResolveRunnerPath();
        var processStartInfo = BuildStartInfo(runnerExecutablePath);
        StartRunnerInBackground(processStartInfo);

        return Ok(new
        {
            message = "Runner started."
        });
    }

    [HttpGet("testrunner/status")]
    public ActionResult GetRunnerStatus()
    {
        lock (RunnerStateLock)
        {
            return Ok(new
            {
                isRunning = _isRunnerRunning,
                processId = _currentProcessId,
                startedAtUtc = _startedAtUtc,
                lastCompletedAtUtc = _lastCompletedAtUtc,
                lastExitCode = _lastExitCode,
                lastError = _lastError
            });
        }
    }

    [HttpPost("testresults")]
    public async Task<ActionResult<TestResultRequest>> Post(
        [FromBody] TestResultRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.StudentName))
        {
            return ProblemBadRequest("studentName is required.");
        }

        var submission = await ResolveSubmissionAsync(request.StudentName, cancellationToken);
        if (submission is null)
        {
            return ProblemNotFound($"Submission not found for studentName '{request.StudentName}'.");
        }

        var testResult = await _db.TestResults.FirstOrDefaultAsync(
            x => x.SubmissionId == submission.Id,
            cancellationToken);

        if (testResult is null)
        {
            testResult = new Infrastructure.TestResult
            {
                SubmissionId = submission.Id
            };
            _db.TestResults.Add(testResult);
        }

        testResult.Score = request.Score;
        testResult.ReportPath = request.ReportFilePath;

        await _db.SaveChangesAsync(cancellationToken);
        await _hubContext.Clients.All.SendAsync("ReceiveTestResult", request, cancellationToken);

        return Ok(request);
    }

    private string ResolveRunnerPath()
    {
        var configuredPath = _configuration["Runner:ExecutablePath"]
            ?? Environment.GetEnvironmentVariable("RUNNER_EXECUTABLE_PATH");

        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            throw new InvalidOperationException("Runner path is missing. Configure Runner:ExecutablePath or RUNNER_EXECUTABLE_PATH.");
        }

        var fullPath = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.GetFullPath(Path.Combine(_environment.ContentRootPath, configuredPath));

        if (!System.IO.File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Runner executable was not found at '{fullPath}'.");
        }

        return fullPath;
    }

    private static ProcessStartInfo BuildStartInfo(string runnerExecutablePath)
    {
        var extension = Path.GetExtension(runnerExecutablePath);
        if (extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            return new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --project \"{runnerExecutablePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
        }

        if (extension.Equals(".dll", StringComparison.OrdinalIgnoreCase))
        {
            return new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"\"{runnerExecutablePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
        }

        return new ProcessStartInfo
        {
            FileName = runnerExecutablePath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
    }

    private async Task<Infrastructure.Submission?> ResolveSubmissionAsync(string studentName, CancellationToken cancellationToken)
    {
        var normalized = studentName.Trim();

        var submissionIdText = normalized.Split('_', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();
        if (int.TryParse(submissionIdText, out var submissionId))
        {
            var byId = await _db.Submissions.FirstOrDefaultAsync(s => s.Id == submissionId, cancellationToken);
            if (byId is not null)
            {
                return byId;
            }
        }

        var byStudentCode = await _db.Submissions
            .OrderByDescending(s => s.Id)
            .FirstOrDefaultAsync(s => s.StudentCode == normalized, cancellationToken);
        if (byStudentCode is not null)
        {
            return byStudentCode;
        }

        var byStudentName = await _db.Submissions
            .OrderByDescending(s => s.Id)
            .FirstOrDefaultAsync(s => s.StudentName == normalized, cancellationToken);
        if (byStudentName is not null)
        {
            return byStudentName;
        }

        return await _db.Submissions
            .OrderByDescending(s => s.Id)
            .FirstOrDefaultAsync(s => EF.Functions.Like(s.FilePath, $"%{normalized}%"), cancellationToken);
    }

    private void StartRunnerInBackground(ProcessStartInfo startInfo)
    {
        _ = Task.Run(async () =>
        {
            int? processId = null;
            try
            {
                using var process = new Process { StartInfo = startInfo };
                process.Start();
                processId = process.Id;

                lock (RunnerStateLock)
                {
                    _isRunnerRunning = true;
                    _currentProcessId = processId;
                    _startedAtUtc = DateTimeOffset.UtcNow;
                    _lastCompletedAtUtc = null;
                    _lastExitCode = null;
                    _lastError = null;
                }

                await _hubContext.Clients.All.SendAsync("RunnerStatusChanged", new
                {
                    isRunning = true,
                    processId,
                    startedAtUtc = _startedAtUtc
                });

                var stdOutTask = process.StandardOutput.ReadToEndAsync();
                var stdErrTask = process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                var stdOut = await stdOutTask;
                var stdErr = await stdErrTask;

                if (process.ExitCode == 0)
                {
                    _logger.LogInformation("Runner finished successfully.");

                    lock (RunnerStateLock)
                    {
                        _isRunnerRunning = false;
                        _currentProcessId = null;
                        _lastCompletedAtUtc = DateTimeOffset.UtcNow;
                        _lastExitCode = process.ExitCode;
                        _lastError = null;
                    }

                    await _hubContext.Clients.All.SendAsync("RunnerCompleted", new
                    {
                        status = "ok",
                        output = stdOut
                    });

                    await _hubContext.Clients.All.SendAsync("RunnerStatusChanged", new
                    {
                        isRunning = false,
                        processId = (int?)null,
                        lastCompletedAtUtc = _lastCompletedAtUtc,
                        lastExitCode = _lastExitCode,
                        lastError = _lastError
                    });
                }
                else
                {
                    _logger.LogWarning("Runner failed. ExitCode: {ExitCode}. Error: {Error}", process.ExitCode, stdErr);

                    lock (RunnerStateLock)
                    {
                        _isRunnerRunning = false;
                        _currentProcessId = null;
                        _lastCompletedAtUtc = DateTimeOffset.UtcNow;
                        _lastExitCode = process.ExitCode;
                        _lastError = stdErr;
                    }

                    await _hubContext.Clients.All.SendAsync("RunnerCompleted", new
                    {
                        status = "build false",
                        error = stdErr
                    });

                    await _hubContext.Clients.All.SendAsync("RunnerStatusChanged", new
                    {
                        isRunning = false,
                        processId = (int?)null,
                        lastCompletedAtUtc = _lastCompletedAtUtc,
                        lastExitCode = _lastExitCode,
                        lastError = _lastError
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute runner.");

                lock (RunnerStateLock)
                {
                    _isRunnerRunning = false;
                    _currentProcessId = null;
                    _lastCompletedAtUtc = DateTimeOffset.UtcNow;
                    _lastExitCode = -1;
                    _lastError = ex.Message;
                }

                await _hubContext.Clients.All.SendAsync("RunnerStatusChanged", new
                {
                    isRunning = false,
                    processId = (int?)null,
                    lastCompletedAtUtc = _lastCompletedAtUtc,
                    lastExitCode = _lastExitCode,
                    lastError = _lastError
                });
            }
        });
    }
}
