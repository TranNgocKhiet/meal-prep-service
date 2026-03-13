using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MealPrepService.DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryManAssignmentToDeliverySchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DeliveryManId",
                table: "DeliverySchedules",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeliverySchedules_DeliveryManId",
                table: "DeliverySchedules",
                column: "DeliveryManId");

            migrationBuilder.AddForeignKey(
                name: "FK_DeliverySchedules_Accounts_DeliveryManId",
                table: "DeliverySchedules",
                column: "DeliveryManId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeliverySchedules_Accounts_DeliveryManId",
                table: "DeliverySchedules");

            migrationBuilder.DropIndex(
                name: "IX_DeliverySchedules_DeliveryManId",
                table: "DeliverySchedules");

            migrationBuilder.DropColumn(
                name: "DeliveryManId",
                table: "DeliverySchedules");
        }
    }
}
