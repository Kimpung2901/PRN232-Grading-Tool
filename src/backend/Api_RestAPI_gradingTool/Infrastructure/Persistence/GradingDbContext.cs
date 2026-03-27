using System;
using Application.Contracts.Grading;
using Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public partial class GradingDbContext : DbContext, IGradingDbContext
{
    public GradingDbContext()
    {
    }

    public GradingDbContext(DbContextOptions<GradingDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Exam> Exams { get; set; }

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
            entity.HasKey(e => e.ExamId);

            entity.Property(e => e.ExamName)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.SqlScriptPath)
                .HasMaxLength(500);
        });

        modelBuilder.Entity<TestCase>(entity =>
        {
            entity.HasIndex(e => e.ExamId, "IX_TestCases_ExamId");

            entity.Property(e => e.FilePath)
                .HasMaxLength(500)
                .IsRequired();

            entity.HasOne(d => d.Exam).WithMany(p => p.TestCases)
                .HasForeignKey(d => d.ExamId)
                .HasConstraintName("FK_TestCases_Exams");
        });

        modelBuilder.Entity<Submission>(entity =>
        {
            entity.HasIndex(e => e.ExamId, "IX_Submissions_ExamId");

            entity.Property(e => e.StudentName)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.StudentCode)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.FilePath)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(e => e.Status)
                .HasDefaultValue(0);

            entity.HasOne(d => d.Exam).WithMany(p => p.Submissions)
                .HasForeignKey(d => d.ExamId)
                .HasConstraintName("FK_Submissions_Exams");
        });

        modelBuilder.Entity<TestResult>(entity =>
        {
            entity.HasIndex(e => e.SubmissionId, "IX_TestResults_SubmissionId");

            entity.Property(e => e.Score)
                .HasColumnType("decimal(8, 2)");

            entity.Property(e => e.ReportPath)
                .HasMaxLength(500);

            entity.HasOne(d => d.Submission).WithMany(p => p.TestResults)
                .HasForeignKey(d => d.SubmissionId)
                .HasConstraintName("FK_TestResults_Submissions");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
