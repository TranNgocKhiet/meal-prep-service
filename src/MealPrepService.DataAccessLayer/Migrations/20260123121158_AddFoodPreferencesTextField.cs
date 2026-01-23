using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MealPrepService.DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddFoodPreferencesTextField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HealthProfileFoodPreferences");

            migrationBuilder.AddColumn<string>(
                name: "FoodPreferences",
                table: "HealthProfiles",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FoodPreferences",
                table: "HealthProfiles");

            migrationBuilder.CreateTable(
                name: "HealthProfileFoodPreferences",
                columns: table => new
                {
                    FoodPreferencesId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HealthProfilesId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HealthProfileFoodPreferences", x => new { x.FoodPreferencesId, x.HealthProfilesId });
                    table.ForeignKey(
                        name: "FK_HealthProfileFoodPreferences_FoodPreferences_FoodPreferencesId",
                        column: x => x.FoodPreferencesId,
                        principalTable: "FoodPreferences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HealthProfileFoodPreferences_HealthProfiles_HealthProfilesId",
                        column: x => x.HealthProfilesId,
                        principalTable: "HealthProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HealthProfileFoodPreferences_HealthProfilesId",
                table: "HealthProfileFoodPreferences",
                column: "HealthProfilesId");
        }
    }
}
