using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewLaserProject.Migrations.WorkTimeDb
{
    /// <inheritdoc />
    public partial class ChangedSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ProcessTimeLogs",
                table: "ProcessTimeLogs");

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "ProcessTimeLogs");

            migrationBuilder.DropColumn(
                name: "IsProcess",
                table: "ProcessTimeLogs");

            migrationBuilder.DropColumn(
                name: "MaterialName",
                table: "ProcessTimeLogs");

            migrationBuilder.DropColumn(
                name: "MaterialThickness",
                table: "ProcessTimeLogs");

            migrationBuilder.DropColumn(
                name: "Success",
                table: "ProcessTimeLogs");

            migrationBuilder.DropColumn(
                name: "TechnologyName",
                table: "ProcessTimeLogs");

            migrationBuilder.DropColumn(
                name: "YieldTime",
                table: "ProcessTimeLogs");

            migrationBuilder.RenameTable(
                name: "ProcessTimeLogs",
                newName: "WorkTimeLogs");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WorkTimeLogs",
                table: "WorkTimeLogs",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "ProcTimeLogs",
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
                    YieldTime = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    Success = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExceptionMessage = table.Column<string>(type: "TEXT", nullable: true),
                    WorkTimeLogId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcTimeLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcTimeLogs_WorkTimeLogs_WorkTimeLogId",
                        column: x => x.WorkTimeLogId,
                        principalTable: "WorkTimeLogs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProcTimeLogs_WorkTimeLogId",
                table: "ProcTimeLogs",
                column: "WorkTimeLogId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProcTimeLogs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WorkTimeLogs",
                table: "WorkTimeLogs");

            migrationBuilder.RenameTable(
                name: "WorkTimeLogs",
                newName: "ProcessTimeLogs");

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "ProcessTimeLogs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsProcess",
                table: "ProcessTimeLogs",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MaterialName",
                table: "ProcessTimeLogs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "MaterialThickness",
                table: "ProcessTimeLogs",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<bool>(
                name: "Success",
                table: "ProcessTimeLogs",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TechnologyName",
                table: "ProcessTimeLogs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "YieldTime",
                table: "ProcessTimeLogs",
                type: "TEXT",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProcessTimeLogs",
                table: "ProcessTimeLogs",
                column: "Id");
        }
    }
}
