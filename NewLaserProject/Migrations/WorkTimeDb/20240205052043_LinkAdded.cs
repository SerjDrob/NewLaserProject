using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewLaserProject.Migrations.WorkTimeDb
{
    /// <inheritdoc />
    public partial class LinkAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProcTimeLogs_WorkTimeLogs_WorkTimeLogId",
                table: "ProcTimeLogs");

            migrationBuilder.AlterColumn<int>(
                name: "WorkTimeLogId",
                table: "ProcTimeLogs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ProcTimeLogs_WorkTimeLogs_WorkTimeLogId",
                table: "ProcTimeLogs",
                column: "WorkTimeLogId",
                principalTable: "WorkTimeLogs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProcTimeLogs_WorkTimeLogs_WorkTimeLogId",
                table: "ProcTimeLogs");

            migrationBuilder.AlterColumn<int>(
                name: "WorkTimeLogId",
                table: "ProcTimeLogs",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddForeignKey(
                name: "FK_ProcTimeLogs_WorkTimeLogs_WorkTimeLogId",
                table: "ProcTimeLogs",
                column: "WorkTimeLogId",
                principalTable: "WorkTimeLogs",
                principalColumn: "Id");
        }
    }
}
