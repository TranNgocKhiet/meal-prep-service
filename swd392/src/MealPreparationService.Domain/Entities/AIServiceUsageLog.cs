namespace MealPreparationService.Domain.Entities;

public class AIServiceUsageLog : BaseEntity
{
    public string OperationType { get; set; } = string.Empty; // MealPlan Generation AI | Nutrition Analysis
    public DateTime Timestamp { get; set; }
    public string Status { get; set; } = string.Empty; // Success | Failed

    public string CustomerId { get; set; } = string.Empty;
    public Account Customer { get; set; } = null!;

    public string? InputParameters { get; set; }
    public string? OutputSummary { get; set; }
    public string? ErrorMessage { get; set; }
    public string? StackTrace { get; set; }

    public int ExecutionDurationMs { get; set; }
    public int CreditsUsed { get; set; }
}
