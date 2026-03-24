using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MealPreparationService.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddAIServiceUsageLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AIServiceUsageLogs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OperationType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CustomerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    InputParameters = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OutputSummary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StackTrace = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExecutionDurationMs = table.Column<int>(type: "int", nullable: false),
                    CreditsUsed = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIServiceUsageLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AIServiceUsageLogs_Accounts_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AIServiceUsageLogs_CustomerId",
                table: "AIServiceUsageLogs",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_AIServiceUsageLogs_OperationType_Timestamp",
                table: "AIServiceUsageLogs",
                columns: new[] { "OperationType", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_AIServiceUsageLogs_Timestamp",
                table: "AIServiceUsageLogs",
                column: "Timestamp");

            // Backfill historical AI meal plan usage from existing AI-generated meal plans.
            migrationBuilder.Sql(@"
INSERT INTO [AIServiceUsageLogs]
    ([Id], [OperationType], [Timestamp], [Status], [CustomerId], [InputParameters], [OutputSummary], [ErrorMessage], [StackTrace], [ExecutionDurationMs], [CreditsUsed])
SELECT
    CONVERT(nvarchar(450), NEWID()),
    'MealPlan Generation AI',
    [mp].[CreatedAt],
    'Success',
    [mp].[AccountId],
    CONCAT('{""durationDays"":', DATEDIFF(day, [mp].[StartDate], [mp].[EndDate]) + 1, ',""startDate"":""', CONVERT(varchar(33), [mp].[StartDate], 127), '""', ',""endDate"":""', CONVERT(varchar(33), [mp].[EndDate], 127), '""', '}'),
    CONCAT('{""success"":true,""mealPlanId"":""', [mp].[Id], '""}'),
    NULL,
    NULL,
    0,
    1
FROM [MealPlans] AS [mp]
WHERE [mp].[IsAiGenerated] = CAST(1 AS bit)
  AND NOT EXISTS (
      SELECT 1
      FROM [AIServiceUsageLogs] AS [l]
      WHERE [l].[OperationType] = 'MealPlan Generation AI'
        AND [l].[CustomerId] = [mp].[AccountId]
        AND [l].[Timestamp] = [mp].[CreatedAt]
  );
");

            // Backfill historical nutrition usage from AI credit transactions that were previously used as proxy analytics.
            migrationBuilder.Sql(@"
INSERT INTO [AIServiceUsageLogs]
    ([Id], [OperationType], [Timestamp], [Status], [CustomerId], [InputParameters], [OutputSummary], [ErrorMessage], [StackTrace], [ExecutionDurationMs], [CreditsUsed])
SELECT
    CONVERT(nvarchar(450), NEWID()),
    'Nutrition Analysis',
    [tx].[CreatedAt],
    'Success',
    [tx].[AccountId],
    '{""source"":""legacy-ai-credit-transaction""}',
    '{""success"":true}',
    NULL,
    NULL,
    0,
    1
FROM [AIcreditTransactions] AS [tx]
WHERE NOT EXISTS (
      SELECT 1
      FROM [AIServiceUsageLogs] AS [l]
      WHERE [l].[OperationType] = 'Nutrition Analysis'
        AND [l].[CustomerId] = [tx].[AccountId]
        AND [l].[Timestamp] = [tx].[CreatedAt]
  );
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AIServiceUsageLogs");
        }
    }
}
