using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SimplifySchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Exams_ExamSessions",
                table: "Exams");

            migrationBuilder.DropForeignKey(
                name: "FK_Submissions_Exams",
                table: "Submissions");

            migrationBuilder.DropForeignKey(
                name: "FK_TestCases_Dependency",
                table: "TestCases");

            migrationBuilder.DropForeignKey(
                name: "FK_TestCases_Exams",
                table: "TestCases");

            migrationBuilder.DropForeignKey(
                name: "FK_TestResults_TestCases",
                table: "TestResults");

            migrationBuilder.DropTable(
                name: "ExamSessions");

            migrationBuilder.DropTable(
                name: "Semesters");

            migrationBuilder.DropIndex(
                name: "IX_TestResults_TestCaseId",
                table: "TestResults");

            migrationBuilder.DropUniqueConstraint(
                name: "UQ_TestResults_Submission_TestCase",
                table: "TestResults");

            migrationBuilder.DropIndex(
                name: "IX_TestCases_DependencyTestCaseId",
                table: "TestCases");

            migrationBuilder.DropIndex(
                name: "UQ_TestCases_Exam_Name",
                table: "TestCases");

            migrationBuilder.DropIndex(
                name: "UQ_TestCases_Exam_PostmanItemId",
                table: "TestCases");

            migrationBuilder.DropIndex(
                name: "IX_Submissions_Status",
                table: "Submissions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Exams",
                table: "Exams");

            migrationBuilder.DropIndex(
                name: "IX_Exams_SessionId",
                table: "Exams");

            migrationBuilder.DropIndex(
                name: "UQ_Exams_Session_Name",
                table: "Exams");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "TestResults");

            migrationBuilder.DropColumn(
                name: "IsPassed",
                table: "TestResults");

            migrationBuilder.DropColumn(
                name: "IsSkipped",
                table: "TestResults");

            migrationBuilder.DropColumn(
                name: "Log",
                table: "TestResults");

            migrationBuilder.DropColumn(
                name: "TestCaseId",
                table: "TestResults");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "TestCases");

            migrationBuilder.DropColumn(
                name: "DependencyTestCaseId",
                table: "TestCases");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "TestCases");

            migrationBuilder.DropColumn(
                name: "PostmanItemId",
                table: "TestCases");

            migrationBuilder.DropColumn(
                name: "Score",
                table: "TestCases");

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "FilePath",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "LastError",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "TotalScore",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "CollectionFilePath",
                table: "Exams");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Exams");

            migrationBuilder.DropColumn(
                name: "DatabaseFilePath",
                table: "Exams");

            migrationBuilder.RenameColumn(
                name: "EarnedScore",
                table: "TestResults",
                newName: "Score");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Exams",
                newName: "ExamId");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Exams",
                newName: "ExamName");

            migrationBuilder.DropColumn(
                name: "SessionId",
                table: "Exams");

            migrationBuilder.AddColumn<string>(
                name: "ReportPath",
                table: "TestResults",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FilePath",
                table: "TestCases",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StudentCode",
                table: "Submissions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Exams",
                table: "Exams",
                column: "ExamId");

            migrationBuilder.AddForeignKey(
                name: "FK_Submissions_Exams",
                table: "Submissions",
                column: "ExamId",
                principalTable: "Exams",
                principalColumn: "ExamId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TestCases_Exams",
                table: "TestCases",
                column: "ExamId",
                principalTable: "Exams",
                principalColumn: "ExamId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Submissions_Exams",
                table: "Submissions");

            migrationBuilder.DropForeignKey(
                name: "FK_TestCases_Exams",
                table: "TestCases");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Exams",
                table: "Exams");

            migrationBuilder.DropColumn(
                name: "ReportPath",
                table: "TestResults");

            migrationBuilder.DropColumn(
                name: "FilePath",
                table: "TestCases");

            migrationBuilder.DropColumn(
                name: "StudentCode",
                table: "Submissions");

            migrationBuilder.RenameColumn(
                name: "Score",
                table: "TestResults",
                newName: "EarnedScore");

            migrationBuilder.RenameColumn(
                name: "ExamName",
                table: "Exams",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "ExamId",
                table: "Exams",
                newName: "Id");

            migrationBuilder.AddColumn<int>(
                name: "SessionId",
                table: "Exams",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "TestResults",
                type: "datetime2(0)",
                precision: 0,
                nullable: false,
                defaultValueSql: "(sysutcdatetime())");

            migrationBuilder.AddColumn<bool>(
                name: "IsPassed",
                table: "TestResults",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSkipped",
                table: "TestResults",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Log",
                table: "TestResults",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TestCaseId",
                table: "TestResults",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "TestCases",
                type: "datetime2(0)",
                precision: 0,
                nullable: false,
                defaultValueSql: "(sysutcdatetime())");

            migrationBuilder.AddColumn<int>(
                name: "DependencyTestCaseId",
                table: "TestCases",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "TestCases",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PostmanItemId",
                table: "TestCases",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Score",
                table: "TestCases",
                type: "decimal(6,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "Submissions",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FilePath",
                table: "Submissions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastError",
                table: "Submissions",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Submissions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                table: "Submissions",
                type: "datetime2(0)",
                precision: 0,
                nullable: false,
                defaultValueSql: "(sysutcdatetime())");

            migrationBuilder.AddColumn<decimal>(
                name: "TotalScore",
                table: "Submissions",
                type: "decimal(8,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "CollectionFilePath",
                table: "Exams",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Exams",
                type: "datetime2(0)",
                precision: 0,
                nullable: false,
                defaultValueSql: "(sysutcdatetime())");

            migrationBuilder.AddColumn<string>(
                name: "DatabaseFilePath",
                table: "Exams",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Exams",
                table: "Exams",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Semesters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Semesters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExamSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SemesterId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamSessions_Semesters",
                        column: x => x.SemesterId,
                        principalTable: "Semesters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TestResults_TestCaseId",
                table: "TestResults",
                column: "TestCaseId");

            migrationBuilder.CreateIndex(
                name: "UQ_TestResults_Submission_TestCase",
                table: "TestResults",
                columns: new[] { "SubmissionId", "TestCaseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TestCases_DependencyTestCaseId",
                table: "TestCases",
                column: "DependencyTestCaseId");

            migrationBuilder.CreateIndex(
                name: "UQ_TestCases_Exam_Name",
                table: "TestCases",
                columns: new[] { "ExamId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_TestCases_Exam_PostmanItemId",
                table: "TestCases",
                columns: new[] { "ExamId", "PostmanItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_Status",
                table: "Submissions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Exams_SessionId",
                table: "Exams",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "UQ_Exams_Session_Name",
                table: "Exams",
                columns: new[] { "SessionId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamSessions_SemesterId",
                table: "ExamSessions",
                column: "SemesterId");

            migrationBuilder.CreateIndex(
                name: "UQ_ExamSessions_Semester_Name",
                table: "ExamSessions",
                columns: new[] { "SemesterId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Semesters_Name",
                table: "Semesters",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Exams_ExamSessions",
                table: "Exams",
                column: "SessionId",
                principalTable: "ExamSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Submissions_Exams",
                table: "Submissions",
                column: "ExamId",
                principalTable: "Exams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TestCases_Dependency",
                table: "TestCases",
                column: "DependencyTestCaseId",
                principalTable: "TestCases",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TestCases_Exams",
                table: "TestCases",
                column: "ExamId",
                principalTable: "Exams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TestResults_TestCases",
                table: "TestResults",
                column: "TestCaseId",
                principalTable: "TestCases",
                principalColumn: "Id");
        }
    }
}
