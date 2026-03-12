using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MealPreparationService.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddAddressAndPhoneToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_PaymentGateways_PaymentGatewayId1",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_PaymentGatewayId1",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaymentGatewayId1",
                table: "Orders");

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Orders");

            migrationBuilder.AddColumn<string>(
                name: "PaymentGatewayId1",
                table: "Orders",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PaymentGatewayId1",
                table: "Orders",
                column: "PaymentGatewayId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_PaymentGateways_PaymentGatewayId1",
                table: "Orders",
                column: "PaymentGatewayId1",
                principalTable: "PaymentGateways",
                principalColumn: "Id");
        }
    }
}
