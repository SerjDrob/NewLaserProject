using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewLaserProject.Migrations
{
    public partial class DefaultLayerFilterAdded : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LayerNameFilter",
                table: "DefaultLayerEntityTechnology");

            migrationBuilder.AddColumn<int>(
                name: "DefaultLayerFilterId",
                table: "DefaultLayerEntityTechnology",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "DefaultLayerFilter",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Filter = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DefaultLayerFilter", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DefaultLayerEntityTechnology_DefaultLayerFilterId",
                table: "DefaultLayerEntityTechnology",
                column: "DefaultLayerFilterId");

            migrationBuilder.AddForeignKey(
                name: "FK_DefaultLayerEntityTechnology_DefaultLayerFilter_DefaultLayerFilterId",
                table: "DefaultLayerEntityTechnology",
                column: "DefaultLayerFilterId",
                principalTable: "DefaultLayerFilter",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DefaultLayerEntityTechnology_DefaultLayerFilter_DefaultLayerFilterId",
                table: "DefaultLayerEntityTechnology");

            migrationBuilder.DropTable(
                name: "DefaultLayerFilter");

            migrationBuilder.DropIndex(
                name: "IX_DefaultLayerEntityTechnology_DefaultLayerFilterId",
                table: "DefaultLayerEntityTechnology");

            migrationBuilder.DropColumn(
                name: "DefaultLayerFilterId",
                table: "DefaultLayerEntityTechnology");

            migrationBuilder.AddColumn<string>(
                name: "LayerNameFilter",
                table: "DefaultLayerEntityTechnology",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
