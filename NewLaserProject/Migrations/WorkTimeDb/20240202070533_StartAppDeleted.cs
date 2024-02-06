using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewLaserProject.Migrations.WorkTimeDb
{
    /// <inheritdoc />
    public partial class StartAppDeleted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StartApp",
                table: "ProcessTimeLogs");

            migrationBuilder.DropColumn(
                name: "StopApp",
                table: "ProcessTimeLogs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "StartApp",
                table: "ProcessTimeLogs",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "StopApp",
                table: "ProcessTimeLogs",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
