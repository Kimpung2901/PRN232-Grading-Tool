using System;
using System.Collections.Generic;
using Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public partial class GradingDbContext : DbContext
{
    public GradingDbContext()
    {
    }

    public GradingDbContext(DbContextOptions<GradingDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Exam> Exams { get; set; }

    public virtual DbSet<ExamSession> ExamSessions { get; set; }

    public virtual DbSet<Semester> Semesters { get; set; }

    public virtual DbSet<Submission> Submissions { get; set; }

    public virtual DbSet<TestCase> TestCases { get; set; }

    public virtual DbSet<TestResult> TestResults { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured)
        {
            return;
        }

        var connectionString = Environment.GetEnvironmentVariable("GRADING_DB_CONNECTION");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            optionsBuilder.UseSqlServer(connectionString);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Exam>(entity =>
        {
            entity.HasIndex(e => e.SessionId, "IX_Exams_SessionId");

            entity.Property(e => e.CollectionFilePath).HasMaxLength(500);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.DatabaseFilePath).HasMaxLength(500);
            entity.Property(e => e.Name).HasMaxLength(200);

            entity.HasOne(d => d.Session).WithMany(p => p.Exams)
                .HasForeignKey(d => d.SessionId)
                .HasConstraintName("FK_Exams_ExamSessions");
        });

        modelBuilder.Entity<ExamSession>(entity =>
        {
            entity.HasIndex(e => e.SemesterId, "IX_ExamSessions_SemesterId");

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Name).HasMaxLength(100);

            entity.HasOne(d => d.Semester).WithMany(p => p.ExamSessions)
                .HasForeignKey(d => d.SemesterId)
                .HasConstraintName("FK_ExamSessions_Semesters");
        });

        modelBuilder.Entity<Semester>(entity =>
        {
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<Submission>(entity =>
        {
            entity.HasIndex(e => e.ExamId, "IX_Submissions_ExamId");

            entity.HasIndex(e => e.Status, "IX_Submissions_Status");

            entity.Property(e => e.FilePath).HasMaxLength(500);
            entity.Property(e => e.LastError).HasMaxLength(2000);
            entity.Property(e => e.StudentName).HasMaxLength(200);
            entity.Property(e => e.SubmittedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.TotalScore).HasColumnType("decimal(8, 2)");

            entity.HasOne(d => d.Exam).WithMany(p => p.Submissions)
                .HasForeignKey(d => d.ExamId)
                .HasConstraintName("FK_Submissions_Exams");
        });

        modelBuilder.Entity<TestCase>(entity =>
        {
            entity.HasIndex(e => e.DependencyTestCaseId, "IX_TestCases_DependencyTestCaseId");

            entity.HasIndex(e => e.ExamId, "IX_TestCases_ExamId");

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Score).HasColumnType("decimal(6, 2)");

            entity.HasOne(d => d.DependencyTestCase).WithMany(p => p.InverseDependencyTestCase)
                .HasForeignKey(d => d.DependencyTestCaseId)
                .HasConstraintName("FK_TestCases_Dependency");

            entity.HasOne(d => d.Exam).WithMany(p => p.TestCases)
                .HasForeignKey(d => d.ExamId)
                .HasConstraintName("FK_TestCases_Exams");
        });

        modelBuilder.Entity<TestResult>(entity =>
        {
            entity.HasIndex(e => e.SubmissionId, "IX_TestResults_SubmissionId");

            entity.HasIndex(e => e.TestCaseId, "IX_TestResults_TestCaseId");

            entity.HasIndex(e => new { e.SubmissionId, e.TestCaseId }, "UQ_TestResults_Submission_TestCase").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.EarnedScore).HasColumnType("decimal(8, 2)");

            entity.HasOne(d => d.Submission).WithMany(p => p.TestResults)
                .HasForeignKey(d => d.SubmissionId)
                .HasConstraintName("FK_TestResults_Submissions");

            entity.HasOne(d => d.TestCase).WithMany(p => p.TestResults)
                .HasForeignKey(d => d.TestCaseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TestResults_TestCases");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
