using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewLaserProject.Migrations
{
    /// <inheritdoc />
    public partial class AddRelationofMaterielEntityRule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MaterialEntRule_MaterialId",
                table: "MaterialEntRule");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialEntRule_MaterialId",
                table: "MaterialEntRule",
                column: "MaterialId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MaterialEntRule_MaterialId",
                table: "MaterialEntRule");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialEntRule_MaterialId",
                table: "MaterialEntRule",
                column: "MaterialId");
        }
    }
}
