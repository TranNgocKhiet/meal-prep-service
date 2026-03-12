using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MealPreparationService.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveIsCustomFromRecipe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCustom",
                table: "Recipes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCustom",
                table: "Recipes",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
