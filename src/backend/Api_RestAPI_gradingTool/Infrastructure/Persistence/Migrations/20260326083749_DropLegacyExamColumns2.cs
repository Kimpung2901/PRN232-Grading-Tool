using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropLegacyExamColumns2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndpointSpecFilePath",
                table: "Exams");

            migrationBuilder.DropColumn(
                name: "EnvironmentFilePath",
                table: "Exams");

            migrationBuilder.DropColumn(
                name: "HealthPath",
                table: "Exams");

            migrationBuilder.DropColumn(
                name: "NewmanExtraArgs",
                table: "Exams");

            migrationBuilder.DropColumn(
                name: "SqlCmdPassword",
                table: "Exams");

            migrationBuilder.DropColumn(
                name: "SqlCmdServer",
                table: "Exams");

            migrationBuilder.DropColumn(
                name: "SqlCmdUser",
                table: "Exams");

            migrationBuilder.DropColumn(
                name: "StudentDbConnection",
                table: "Exams");

            migrationBuilder.DropColumn(
                name: "SwaggerPath",
                table: "Exams");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EndpointSpecFilePath",
                table: "Exams",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnvironmentFilePath",
                table: "Exams",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HealthPath",
                table: "Exams",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NewmanExtraArgs",
                table: "Exams",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SqlCmdPassword",
                table: "Exams",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SqlCmdServer",
                table: "Exams",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SqlCmdUser",
                table: "Exams",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StudentDbConnection",
                table: "Exams",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SwaggerPath",
                table: "Exams",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }
    }
}
