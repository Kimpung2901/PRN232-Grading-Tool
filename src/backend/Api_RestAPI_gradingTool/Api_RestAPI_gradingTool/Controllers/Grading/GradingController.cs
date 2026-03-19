using Api_RestAPI_gradingTool.Contracts.Grading;
using Api_RestAPI_gradingTool.Services.Grading;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api_RestAPI_gradingTool.Controllers.Grading;

[Route("api")]
public sealed class GradingController : ApiControllerBase
{
    private readonly GradingDbContext _db;
    private readonly GradingRunnerService _runner;

    public GradingController(GradingDbContext db, GradingRunnerService runner)
    {
        _db = db;
        _runner = runner;
    }

    [HttpPost("submissions/{submissionId:int}/run")]
    public async Task<ActionResult<GradingJobDto>> Run(int submissionId, CancellationToken cancellationToken)
    {
        try
        {
            var job = await _runner.RunAsync(submissionId, cancellationToken);
            return Ok(ToDto(job));
        }
        catch (Exception ex)
        {
            return ProblemBadRequest(ex.Message);
        }
    }

    [HttpGet("submissions/{submissionId:int}/results")]
    public async Task<ActionResult<SubmissionResultDto>> Results(int submissionId, CancellationToken cancellationToken)
    {
        var submission = await _db.Submissions
            .AsNoTracking()
            .Where(s => s.Id == submissionId)
            .Select(s => new
            {
                s.Id,
                s.Status,
                s.TotalScore,
                s.LastError
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (submission is null)
        {
            return ProblemNotFound("Submission not found.");
        }

        var results = await _db.TestResults
            .AsNoTracking()
            .Where(r => r.SubmissionId == submissionId)
            .Select(r => new TestResultItemDto
            {
                TestCaseId = r.TestCaseId,
                TestCaseName = r.TestCase.Name,
                IsPassed = r.IsPassed,
                IsSkipped = r.IsSkipped,
                EarnedScore = r.EarnedScore,
                Log = r.Log
            })
            .ToArrayAsync(cancellationToken);

        return Ok(new SubmissionResultDto
        {
            SubmissionId = submission.Id,
            Status = submission.Status,
            TotalScore = submission.TotalScore,
            LastError = submission.LastError,
            Results = results
        });
    }

    [HttpGet("submissions/{submissionId:int}/logs")]
    public async Task<ActionResult<RunLogDto[]>> Logs(int submissionId, CancellationToken cancellationToken)
    {
        var job = await _db.GradingJobs
            .AsNoTracking()
            .Where(j => j.SubmissionId == submissionId)
            .OrderByDescending(j => j.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (job is null)
        {
            return ProblemNotFound("Grading job not found.");
        }

        var logs = await _db.RunLogs
            .AsNoTracking()
            .Where(l => l.JobId == job.Id)
            .Select(l => new RunLogDto
            {
                Id = l.Id,
                JobId = l.JobId,
                Type = l.Type,
                FilePath = l.FilePath,
                CreatedAt = l.CreatedAt
            })
            .ToArrayAsync(cancellationToken);

        return Ok(logs);
    }

    private static GradingJobDto ToDto(Infrastructure.GradingJob job)
    {
        return new GradingJobDto
        {
            Id = job.Id,
            SubmissionId = job.SubmissionId,
            Status = job.Status,
            CreatedAt = job.CreatedAt,
            StartedAt = job.StartedAt,
            FinishedAt = job.FinishedAt,
            LastError = job.LastError
        };
    }
}
