namespace MealPreparationService.Domain.Entities;

public class NutrientCalculation : BaseEntity
{
    public string EntityId { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty; // Recipe, Meal, MealPlan
    public string NutrientId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTime CalculatedAt { get; set; }
    
    // Navigation properties
    public virtual Nutrient? Nutrient { get; set; }
}
