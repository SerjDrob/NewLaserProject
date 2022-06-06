using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewLaserProject.Migrations
{
    public partial class ToDefaultLayerFilterIsVisibleAdded : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsVisible",
                table: "DefaultLayerFilter",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsVisible",
                table: "DefaultLayerFilter");
        }
    }
}
