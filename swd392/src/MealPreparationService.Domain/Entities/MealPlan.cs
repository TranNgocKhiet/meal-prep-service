namespace MealPreparationService.Domain.Entities;

public class MealPlan : BaseEntity
{
    public string AccountId { get; set; } = string.Empty;
    public Account Account { get; set; } = null!;
    public string PlanName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsAiGenerated { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; }
    
    // Personal Information
    public int? Age { get; set; }
    public decimal? Weight { get; set; }
    public decimal? Height { get; set; }
    public string? Gender { get; set; }
    public string? HealthNote { get; set; }
    public int? CaloriesGoal { get; set; }
    
    // Navigation properties
    public ICollection<Meal> Meals { get; set; } = new List<Meal>();
}
