using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "UQ_Semesters_Name",
                table: "Semesters",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_ExamSessions_Semester_Name",
                table: "ExamSessions",
                columns: new[] { "SemesterId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Exams_Session_Name",
                table: "Exams",
                columns: new[] { "SessionId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ_TestCases_Exam_Name",
                table: "TestCases");

            migrationBuilder.DropIndex(
                name: "UQ_TestCases_Exam_PostmanItemId",
                table: "TestCases");

            migrationBuilder.DropIndex(
                name: "UQ_Semesters_Name",
                table: "Semesters");

            migrationBuilder.DropIndex(
                name: "UQ_ExamSessions_Semester_Name",
                table: "ExamSessions");

            migrationBuilder.DropIndex(
                name: "UQ_Exams_Session_Name",
                table: "Exams");
        }
    }
}
