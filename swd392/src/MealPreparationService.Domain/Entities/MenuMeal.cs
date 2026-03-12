namespace MealPreparationService.Domain.Entities;

public class MenuMeal : BaseEntity
{
    public string MenuId { get; set; } = string.Empty;
    public DailyMenu Menu { get; set; } = null!;
    public int MealTypeId { get; set; }
    public MealType MealType { get; set; } = null!;
    public decimal TotalCalories { get; set; }
    public decimal ProteinG { get; set; }
    public decimal FatG { get; set; }
    public decimal CarbsG { get; set; }
    public decimal Price { get; set; }
    public int AvailableQuantity { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ICollection<MenuMealRecipe> MenuMealRecipes { get; set; } = new List<MenuMealRecipe>();
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}
