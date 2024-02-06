using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewLaserProject.Migrations.WorkTimeDb
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProcessTimeLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FileName = table.Column<string>(type: "TEXT", nullable: true),
                    MaterialName = table.Column<string>(type: "TEXT", nullable: true),
                    TechnologyName = table.Column<string>(type: "TEXT", nullable: true),
                    MaterialThickness = table.Column<double>(type: "REAL", nullable: false),
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    StartApp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    StopApp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    YieldTime = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    Success = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsProcess = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessTimeLogs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProcessTimeLogs");
        }
    }
}
