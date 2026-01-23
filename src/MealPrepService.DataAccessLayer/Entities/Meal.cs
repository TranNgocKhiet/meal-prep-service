namespace MealPrepService.DataAccessLayer.Entities;

public class Meal : BaseEntity
{
    public Guid PlanId { get; set; }
    public string MealType { get; set; } = string.Empty; // breakfast, lunch, dinner
    public DateTime ServeDate { get; set; }
    
    // Navigation properties
    public MealPlan Plan { get; set; } = null!;
    public ICollection<MealRecipe> MealRecipes { get; set; } = new List<MealRecipe>();
}

