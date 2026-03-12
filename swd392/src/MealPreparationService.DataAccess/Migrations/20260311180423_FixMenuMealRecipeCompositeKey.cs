using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MealPreparationService.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class FixMenuMealRecipeCompositeKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_MenuMealRecipes",
                table: "MenuMealRecipes");

            migrationBuilder.DropIndex(
                name: "IX_MenuMealRecipes_MenuMealId",
                table: "MenuMealRecipes");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "MenuMealRecipes");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MenuMealRecipes",
                table: "MenuMealRecipes",
                columns: new[] { "MenuMealId", "RecipeId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_MenuMealRecipes",
                table: "MenuMealRecipes");

            migrationBuilder.AddColumn<string>(
                name: "Id",
                table: "MenuMealRecipes",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MenuMealRecipes",
                table: "MenuMealRecipes",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_MenuMealRecipes_MenuMealId",
                table: "MenuMealRecipes",
                column: "MenuMealId");
        }
    }
}
