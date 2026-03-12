namespace MealPreparationService.Domain.Entities;

public class DailyMenu : BaseEntity
{
    public int StatusId { get; set; }
    public Status Status { get; set; } = null!;
    public DateTime MenuDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ICollection<MenuMeal> MenuMeals { get; set; } = new List<MenuMeal>();
}
