namespace MealPreparationService.Domain.Entities;

public class DailyMenu : BaseEntity
{
    public int StatusId { get; set; }
    public Status Status { get; set; } = null!;
    public DateTime MenuDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public ICollection<MenuMeal> MenuMeals { get; set; } = new List<MenuMeal>();
}



