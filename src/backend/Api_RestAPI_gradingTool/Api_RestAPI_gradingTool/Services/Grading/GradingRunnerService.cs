using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Infrastructure;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Api_RestAPI_gradingTool.Services.Grading;

public sealed class GradingRunnerService
{
    private readonly GradingDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly GradingRunnerOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;

    public GradingRunnerService(
        GradingDbContext db,
        IWebHostEnvironment env,
        IOptions<GradingRunnerOptions> options,
        IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _env = env;
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<GradingJob> RunAsync(int submissionId, CancellationToken cancellationToken)
    {
        var submission = await _db.Submissions
            .Include(s => s.Exam)
            .FirstOrDefaultAsync(s => s.Id == submissionId, cancellationToken);

        if (submission is null)
        {
            throw new InvalidOperationException("Submission not found.");
        }

        if (string.IsNullOrWhiteSpace(submission.FilePath) || !File.Exists(submission.FilePath))
        {
            throw new InvalidOperationException("Submission file is missing.");
        }

        if (submission.Exam is null)
        {
            throw new InvalidOperationException("Exam not found.");
        }

        var exam = submission.Exam;

        if (string.IsNullOrWhiteSpace(exam.CollectionFilePath) || !File.Exists(exam.CollectionFilePath))
        {
            throw new InvalidOperationException("Exam collection.json is missing.");
        }

        var job = new GradingJob
        {
            SubmissionId = submission.Id,
            Status = 1,
            StartedAt = DateTime.UtcNow
        };
        _db.GradingJobs.Add(job);
        submission.Status = 1;
        submission.LastError = null;
        await _db.SaveChangesAsync(cancellationToken);

        var workRoot = Path.Combine(_env.ContentRootPath, _options.WorkingRoot, "submissions", submission.Id.ToString());
        var extractDir = Path.Combine(workRoot, "extracted");
        var logsDir = Path.Combine(workRoot, "logs");
        Directory.CreateDirectory(extractDir);
        Directory.CreateDirectory(logsDir);

        try
        {
            await ExtractSubmissionAsync(submission.FilePath, extractDir, cancellationToken);
            UnblockDownloadedFiles(extractDir);
            DeleteBuildArtifacts(extractDir);

            var slnPath = FindSolution(extractDir);
            if (slnPath is null)
            {
                var nestedRoot = TryExtractNestedArchive(extractDir, logsDir, cancellationToken);
                if (nestedRoot is not null)
                {
                    UnblockDownloadedFiles(nestedRoot);
                    DeleteBuildArtifacts(nestedRoot);
                    slnPath = FindSolution(nestedRoot);
                }
            }
            if (slnPath is null)
            {
                throw new InvalidOperationException("Solution (.sln) not found after extract.");
            }

            var webProjectPath = FindWebProject(Path.GetDirectoryName(slnPath) ?? extractDir);
            if (webProjectPath is null)
            {
                throw new InvalidOperationException("Web API project (.csproj with Microsoft.NET.Sdk.Web) not found.");
            }

            await RunDatabaseScriptAsync(exam, logsDir, cancellationToken);

            var restoreLog = Path.Combine(logsDir, "restore.log");
            var buildLog = Path.Combine(logsDir, "build.log");
            var runLog = Path.Combine(logsDir, "run.log");
            var newmanLog = Path.Combine(logsDir, "newman.log");

            var restoreCode = await RunWithTimeout(_options.RestoreTimeoutSeconds,
                ct => ProcessRunner.RunAsync(_options.DotnetPath, $"restore \"{slnPath}\"", Path.GetDirectoryName(slnPath) ?? extractDir, restoreLog, ct),
                cancellationToken);
            if (restoreCode != 0) throw new InvalidOperationException("dotnet restore failed.");

            var buildCode = await RunWithTimeout(_options.BuildTimeoutSeconds,
                ct => ProcessRunner.RunAsync(_options.DotnetPath, $"build \"{slnPath}\" -c Release", Path.GetDirectoryName(slnPath) ?? extractDir, buildLog, ct),
                cancellationToken);
            if (buildCode != 0) throw new InvalidOperationException("dotnet build failed.");

            var port = GetRandomPort();
            var baseUrl = $"http://localhost:{port}";

            OverrideStudentConnectionStrings(webProjectPath, exam);
            using var apiProcess = StartApiProcess(webProjectPath, port, runLog, exam);
            try
            {
                var healthy = await WaitForHealthyAsync(baseUrl, exam.HealthPath, cancellationToken);
                if (!healthy) throw new InvalidOperationException("API did not become healthy.");

                await RunSpecCheckAsync(exam, baseUrl, logsDir, cancellationToken);

                var reportPath = Path.Combine(logsDir, "newman-report.json");
                var newmanArgs = BuildNewmanArgs(exam, baseUrl, reportPath);
                var newmanCode = await RunWithTimeout(_options.NewmanTimeoutSeconds,
                    ct => ProcessRunner.RunAsync(_options.NewmanPath, newmanArgs, Path.GetDirectoryName(exam.CollectionFilePath) ?? _env.ContentRootPath, newmanLog, ct),
                    cancellationToken);
                if (newmanCode != 0 && !File.Exists(reportPath))
                {
                    throw new InvalidOperationException("newman run failed.");
                }

                await PersistResultsAsync(submission, reportPath, cancellationToken);
                AddRunLogs(job.Id, logsDir);
            }
            finally
            {
                TryStopProcess(apiProcess);
            }

            job.Status = 2;
            job.FinishedAt = DateTime.UtcNow;
            submission.Status = 2;
            await _db.SaveChangesAsync(cancellationToken);

            return job;
        }
        catch (Exception ex)
        {
            job.Status = 3;
            job.FinishedAt = DateTime.UtcNow;
            job.LastError = ex.Message;
            submission.Status = 3;
            submission.LastError = ex.Message;
            AddRunLogs(job.Id, logsDir);
            await _db.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    private async Task PersistResultsAsync(Submission submission, string reportPath, CancellationToken cancellationToken)
    {
        var executions = NewmanReportParser.ParseExecutions(reportPath)
            .GroupBy(x => x.ItemId)
            .ToDictionary(x => x.Key, x => x.First());

        var testCases = await _db.TestCases
            .Where(t => t.ExamId == submission.ExamId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var dependencyMap = testCases.ToDictionary(t => t.Id, t => t.DependencyTestCaseId);
        var results = new List<TestResult>();

        foreach (var testCase in testCases)
        {
            var res = new TestResult
            {
                SubmissionId = submission.Id,
                TestCaseId = testCase.Id,
                IsPassed = false,
                IsSkipped = false,
                EarnedScore = 0m,
                Log = null
            };

            var shouldSkip = false;
            if (testCase.DependencyTestCaseId.HasValue)
            {
                var depId = testCase.DependencyTestCaseId.Value;
                var depResult = results.FirstOrDefault(r => r.TestCaseId == depId);
                if (depResult is not null && (!depResult.IsPassed))
                {
                    shouldSkip = true;
                }
            }

            if (shouldSkip)
            {
                res.IsSkipped = true;
                res.Log = "Skipped due to dependency failure.";
            }
            else if (executions.TryGetValue(testCase.PostmanItemId, out var exec))
            {
                res.IsPassed = exec.Passed;
                res.EarnedScore = exec.Passed ? testCase.Score : 0m;
                res.Log = exec.Log;
            }
            else
            {
                res.IsPassed = false;
                res.Log = "Execution not found in Newman report.";
            }

            results.Add(res);
        }

        _db.TestResults.RemoveRange(_db.TestResults.Where(r => r.SubmissionId == submission.Id));
        _db.TestResults.AddRange(results);

        submission.TotalScore = results.Sum(r => r.EarnedScore);
    }

    private static int GetRandomPort()
    {
        var rnd = Random.Shared.Next(5000, 6000);
        return rnd;
    }

    private Process StartApiProcess(string projectPath, int port, string logPath, Exam exam)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(logPath) ?? _env.ContentRootPath);

        var psi = new ProcessStartInfo
        {
            FileName = _options.DotnetPath,
            Arguments = $"run --no-launch-profile --project \"{projectPath}\" --urls http://localhost:{port}",
            WorkingDirectory = Path.GetDirectoryName(projectPath) ?? _env.ContentRootPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        ApplyStudentDbOverride(psi, exam);

        var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        var logStream = new FileStream(logPath, FileMode.Create, FileAccess.Write, FileShare.Read);
        var writer = new StreamWriter(logStream) { AutoFlush = true };

        process.OutputDataReceived += (_, e) => { if (e.Data != null) writer.WriteLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) writer.WriteLine(e.Data); };

        if (!process.Start())
        {
            writer.WriteLine("Failed to start API process.");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        process.Exited += (_, __) =>
        {
            writer.Dispose();
            logStream.Dispose();
        };

        return process;
    }

    private async Task<bool> WaitForHealthyAsync(string baseUrl, string? healthPath, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient();
        var deadline = DateTime.UtcNow.AddSeconds(_options.HealthCheckSeconds);

        var paths = new List<string>();
        var pathOverride = string.IsNullOrWhiteSpace(healthPath) ? _options.HealthPath : healthPath;
        if (!string.IsNullOrWhiteSpace(pathOverride))
        {
            paths.Add(pathOverride);
        }
        paths.Add("/");
        paths.Add("/swagger");
        paths.Add("/swagger/v1/swagger.json");

        while (DateTime.UtcNow < deadline)
        {
            foreach (var path in paths.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var url = CombineUrl(baseUrl, path);
                try
                {
                    using var response = await client.GetAsync(url, cancellationToken);
                    // Any HTTP response means the app is up (even 401/403/404).
                    return true;
                }
                catch
                {
                    // ignore
                }
            }

            await Task.Delay(1000, cancellationToken);
        }

        return false;
    }

    private static void EnsureEmptyDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
        }
        Directory.CreateDirectory(path);
    }

    private static void UnblockDownloadedFiles(string root)
    {
        if (!OperatingSystem.IsWindows()) return;
        if (!Directory.Exists(root)) return;

        foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            try
            {
                File.Delete(file + ":Zone.Identifier");
            }
            catch
            {
                // ignore if ADS does not exist or cannot be removed
            }
        }
    }

    private static void DeleteBuildArtifacts(string root)
    {
        if (!Directory.Exists(root)) return;

        foreach (var dir in Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories))
        {
            var name = Path.GetFileName(dir);
            if (!string.Equals(name, "bin", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(name, "obj", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                Directory.Delete(dir, true);
            }
            catch
            {
                // ignore cleanup failures
            }
        }
    }

    private async Task ExtractSubmissionAsync(string filePath, string extractDir, CancellationToken cancellationToken)
    {
        EnsureEmptyDirectory(extractDir);

        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        if (ext == ".zip")
        {
            ZipFile.ExtractToDirectory(filePath, extractDir, true);
            return;
        }

        if (ext == ".rar")
        {
            var sevenZip = "7z";
            var args = $"x \"{filePath}\" -o\"{extractDir}\" -y";
            var logPath = Path.Combine(extractDir, "extract.log");
            var code = await ProcessRunner.RunAsync(sevenZip, args, Path.GetDirectoryName(filePath) ?? extractDir, logPath, cancellationToken);
            if (code != 0)
            {
                throw new InvalidOperationException("RAR extraction failed. Ensure 7z is installed.");
            }
            return;
        }

        throw new InvalidOperationException("Unsupported archive. Only .zip/.rar allowed.");
    }

    private static string? FindSolution(string root)
    {
        var exts = new[] { ".sln", ".slnx", ".slnf" };
        var stack = new Stack<string>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var dir = stack.Pop();
            try
            {
                foreach (var ext in exts)
                {
                    foreach (var sln in Directory.EnumerateFiles(dir, $"*{ext}", SearchOption.TopDirectoryOnly))
                    {
                        return sln;
                    }
                }

                foreach (var sub in Directory.EnumerateDirectories(dir, "*", SearchOption.TopDirectoryOnly))
                {
                    var name = Path.GetFileName(sub);
                    if (string.Equals(name, "bin", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(name, "obj", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(name, ".git", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    stack.Push(sub);
                }
            }
            catch
            {
                // ignore unreadable paths
            }
        }

        return null;
    }

    private string? TryExtractNestedArchive(string extractDir, string logsDir, CancellationToken cancellationToken)
    {
        try
        {
            var innerZip = Directory.GetFiles(extractDir, "*.zip", SearchOption.TopDirectoryOnly).FirstOrDefault();
            var innerRar = Directory.GetFiles(extractDir, "*.rar", SearchOption.TopDirectoryOnly).FirstOrDefault();
            var inner = innerZip ?? innerRar;
            if (inner is null) return null;

            var nestedDir = Path.Combine(extractDir, "nested");
            Directory.CreateDirectory(nestedDir);

            var ext = Path.GetExtension(inner).ToLowerInvariant();
            if (ext == ".zip")
            {
                ZipFile.ExtractToDirectory(inner, nestedDir, true);
            }
            else if (ext == ".rar")
            {
                var args = $"x \"{inner}\" -o\"{nestedDir}\" -y";
                var logPath = Path.Combine(logsDir, "extract-nested.log");
                ProcessRunner.RunAsync("7z", args, Path.GetDirectoryName(inner) ?? extractDir, logPath, cancellationToken)
                    .GetAwaiter().GetResult();
            }

            return nestedDir;
        }
        catch
        {
            return null;
        }
    }

    private static string? FindWebProject(string root)
    {
        foreach (var csproj in Directory.GetFiles(root, "*.csproj", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(csproj);
            if (text.Contains("Microsoft.NET.Sdk.Web", StringComparison.OrdinalIgnoreCase))
            {
                return csproj;
            }
        }

        return null;
    }

    private async Task RunDatabaseScriptAsync(Exam exam, string logsDir, CancellationToken cancellationToken)
    {
        var scriptPath = exam.DatabaseFilePath;
        if (string.IsNullOrWhiteSpace(scriptPath) || !File.Exists(scriptPath))
        {
            return;
        }

        var server = string.IsNullOrWhiteSpace(exam.SqlCmdServer) ? _options.SqlCmdServer : exam.SqlCmdServer;
        var user = string.IsNullOrWhiteSpace(exam.SqlCmdUser) ? _options.SqlCmdUser : exam.SqlCmdUser;
        var password = string.IsNullOrWhiteSpace(exam.SqlCmdPassword) ? _options.SqlCmdPassword : exam.SqlCmdPassword;

        if (string.IsNullOrWhiteSpace(server))
        {
            return;
        }

        var logPath = Path.Combine(logsDir, "db.log");
        var args = $"-S {server}";
        if (!string.IsNullOrWhiteSpace(user) && !string.IsNullOrWhiteSpace(password))
        {
            args += $" -U {user} -P {password}";
        }
        else
        {
            args += " -E";
        }
        args += $" -i \"{scriptPath}\"";
        await ProcessRunner.RunAsync(_options.SqlCmdPath ?? "sqlcmd", args, Path.GetDirectoryName(scriptPath) ?? logsDir, logPath, cancellationToken);
    }

    private void AddRunLogs(int jobId, string logsDir)
    {
        if (!Directory.Exists(logsDir)) return;

        var files = Directory.GetFiles(logsDir, "*.log", SearchOption.TopDirectoryOnly)
            .Concat(Directory.GetFiles(logsDir, "*.json", SearchOption.TopDirectoryOnly));

        foreach (var file in files)
        {
            var type = Path.GetFileNameWithoutExtension(file);
            var exists = _db.RunLogs.Any(l => l.JobId == jobId && l.FilePath == file);
            if (exists) continue;

            _db.RunLogs.Add(new RunLog
            {
                JobId = jobId,
                Type = type,
                FilePath = file
            });
        }
    }

    private static async Task<int> RunWithTimeout(int seconds, Func<CancellationToken, Task<int>> action, CancellationToken cancellationToken)
    {
        if (seconds <= 0) return await action(cancellationToken);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(seconds));
        return await action(cts.Token);
    }

    private async Task RunSpecCheckAsync(Exam exam, string baseUrl, string logsDir, CancellationToken cancellationToken)
    {
        if (!_options.EnableSpecCheck) return;

        var specPath = exam.EndpointSpecFilePath;
        if (string.IsNullOrWhiteSpace(specPath) || !File.Exists(specPath)) return;

        var swaggerPath = string.IsNullOrWhiteSpace(exam.SwaggerPath) ? _options.SwaggerPath : exam.SwaggerPath;
        var swaggerUrl = CombineUrl(baseUrl, swaggerPath);
        var logPath = Path.Combine(logsDir, "spec-check.log");

        try
        {
            var client = _httpClientFactory.CreateClient();
            var swaggerJson = await client.GetStringAsync(swaggerUrl, cancellationToken);

            var expected = LoadSpecEndpoints(specPath);
            var actual = LoadSwaggerEndpoints(swaggerJson);

            var sb = new StringBuilder();
            sb.AppendLine($"Swagger: {swaggerUrl}");
            sb.AppendLine($"Expected endpoints: {expected.Count}");
            sb.AppendLine($"Actual endpoints: {actual.Count}");

            foreach (var exp in expected)
            {
                if (!actual.Contains(exp))
                {
                    sb.AppendLine($"MISSING {exp.method} {exp.route}");
                }
            }

            await File.WriteAllTextAsync(logPath, sb.ToString(), cancellationToken);
        }
        catch (Exception ex)
        {
            await File.WriteAllTextAsync(logPath, $"Spec check failed: {ex.Message}", cancellationToken);
        }
    }

    private static HashSet<(string method, string route)> LoadSpecEndpoints(string specPath)
    {
        using var stream = File.OpenRead(specPath);
        using var doc = JsonDocument.Parse(stream);
        var set = new HashSet<(string method, string route)>();

        if (!doc.RootElement.TryGetProperty("endpoints", out var endpoints)) return set;
        foreach (var ep in endpoints.EnumerateArray())
        {
            var method = ep.GetProperty("method").GetString() ?? string.Empty;
            var route = ep.GetProperty("route").GetString() ?? string.Empty;
            set.Add((method.ToUpperInvariant(), route));
        }
        return set;
    }

    private static HashSet<(string method, string route)> LoadSwaggerEndpoints(string swaggerJson)
    {
        using var doc = JsonDocument.Parse(swaggerJson);
        var set = new HashSet<(string method, string route)>();

        if (!doc.RootElement.TryGetProperty("paths", out var paths)) return set;
        foreach (var path in paths.EnumerateObject())
        {
            var route = path.Name;
            foreach (var method in path.Value.EnumerateObject())
            {
                set.Add((method.Name.ToUpperInvariant(), route));
            }
        }
        return set;
    }

    private static string CombineUrl(string baseUrl, string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return baseUrl;
        if (baseUrl.EndsWith("/") && path.StartsWith("/")) return baseUrl.TrimEnd('/') + path;
        if (!baseUrl.EndsWith("/") && !path.StartsWith("/")) return baseUrl + "/" + path;
        return baseUrl + path;
    }

    private static void TryStopProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(true);
            }
        }
        catch
        {
            // ignore
        }
    }

    private void ApplyStudentDbOverride(ProcessStartInfo psi, Exam exam)
    {
        var conn = !string.IsNullOrWhiteSpace(exam.StudentDbConnection)
            ? exam.StudentDbConnection
            : _options.StudentDbConnection;
        if (string.IsNullOrWhiteSpace(conn)) return;

        // Common keys used in student projects
        psi.Environment["ConnectionStrings__Default"] = conn;
        psi.Environment["ConnectionStrings__DefaultConnection"] = conn;
        psi.Environment["ConnectionStrings__MyCnn"] = conn;
    }

    private void OverrideStudentConnectionStrings(string projectPath, Exam exam)
    {
        var conn = !string.IsNullOrWhiteSpace(exam.StudentDbConnection)
            ? exam.StudentDbConnection
            : _options.StudentDbConnection;
        if (string.IsNullOrWhiteSpace(conn)) return;

        var projectDir = Path.GetDirectoryName(projectPath) ?? _env.ContentRootPath;
        var files = Directory.GetFiles(projectDir, "appsettings*.json", SearchOption.TopDirectoryOnly);

        foreach (var file in files)
        {
            try
            {
                var rootNode = JsonNode.Parse(File.ReadAllText(file)) as JsonObject;
                if (rootNode is null) continue;

                var cs = rootNode["ConnectionStrings"] as JsonObject ?? new JsonObject();

                foreach (var key in cs.Select(k => k.Key).ToList())
                {
                    cs[key] = conn;
                }

                cs["Default"] = conn;
                cs["DefaultConnection"] = conn;
                cs["MyCnn"] = conn;

                rootNode["ConnectionStrings"] = cs;
                File.WriteAllText(file, rootNode.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            }
            catch
            {
                // ignore override failures to avoid breaking grading
            }
        }
    }

    private string BuildNewmanArgs(Exam exam, string baseUrl, string reportPath)
    {
        var args = $"run \"{exam.CollectionFilePath}\" --env-var baseUrl={baseUrl} --reporters json --reporter-json-export \"{reportPath}\"";

        if (!string.IsNullOrWhiteSpace(exam.EnvironmentFilePath) && File.Exists(exam.EnvironmentFilePath))
        {
            args += $" --environment \"{exam.EnvironmentFilePath}\"";
        }

        if (!string.IsNullOrWhiteSpace(exam.NewmanExtraArgs))
        {
            args += " " + exam.NewmanExtraArgs.Trim();
        }

        return args;
    }
}


