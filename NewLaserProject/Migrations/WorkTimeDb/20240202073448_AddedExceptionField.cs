using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewLaserProject.Migrations.WorkTimeDb
{
    /// <inheritdoc />
    public partial class AddedExceptionField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExceptionMessage",
                table: "ProcessTimeLogs",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExceptionMessage",
                table: "ProcessTimeLogs");
        }
    }
}
