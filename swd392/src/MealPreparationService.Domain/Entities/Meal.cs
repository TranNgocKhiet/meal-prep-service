namespace MealPreparationService.Domain.Entities;

public class Meal : BaseEntity
{
    public string PlanId { get; set; } = string.Empty;
    public MealPlan MealPlan { get; set; } = null!;
    public int MealTypeId { get; set; }
    public MealType MealType { get; set; } = null!;
    public decimal TotalCalories { get; set; }
    public decimal ProteinG { get; set; }
    public decimal FatG { get; set; }
    public decimal CarbsG { get; set; }
    public DateTime ServerDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool MealFinished { get; set; }
    
    // Navigation properties
    public ICollection<MealRecipe> MealRecipes { get; set; } = new List<MealRecipe>();
}



