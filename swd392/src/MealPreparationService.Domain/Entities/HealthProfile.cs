namespace MealPreparationService.Domain.Entities;

public class HealthProfile : BaseEntity
{
    public string AccountId { get; set; } = string.Empty;
    public Account Account { get; set; } = null!;
    public int Age { get; set; }
    public decimal Weight { get; set; }
    public decimal Height { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string HealthNotes { get; set; } = string.Empty;
    public int CalorieGoal { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ICollection<HealthProfileAllergy> HealthProfileAllergies { get; set; } = new List<HealthProfileAllergy>();
    public ICollection<HealthProfileIngredient> HealthProfileIngredients { get; set; } = new List<HealthProfileIngredient>();
    public ICollection<MealPlan> MealPlans { get; set; } = new List<MealPlan>();
}
