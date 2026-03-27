using Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Application.Contracts.Grading;

public interface IGradingDbContext
{
    DbSet<Exam> Exams { get; }
    DbSet<TestCase> TestCases { get; }
    DbSet<Submission> Submissions { get; }
    DbSet<TestResult> TestResults { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
