using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MealPreparationService.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonalInfoToMealPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Age",
                table: "MealPlans",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CaloriesGoal",
                table: "MealPlans",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "MealPlans",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HealthNote",
                table: "MealPlans",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Height",
                table: "MealPlans",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Weight",
                table: "MealPlans",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Age",
                table: "MealPlans");

            migrationBuilder.DropColumn(
                name: "CaloriesGoal",
                table: "MealPlans");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "MealPlans");

            migrationBuilder.DropColumn(
                name: "HealthNote",
                table: "MealPlans");

            migrationBuilder.DropColumn(
                name: "Height",
                table: "MealPlans");

            migrationBuilder.DropColumn(
                name: "Weight",
                table: "MealPlans");
        }
    }
}
