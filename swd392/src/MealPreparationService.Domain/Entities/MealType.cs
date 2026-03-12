namespace MealPreparationService.Domain.Entities;

public class MealType
{
    public int Id { get; set; }
    public string TypeName { get; set; } = string.Empty;
    
    // Navigation properties
    public ICollection<Meal> Meals { get; set; } = new List<Meal>();
    public ICollection<MenuMeal> MenuMeals { get; set; } = new List<MenuMeal>();
}
