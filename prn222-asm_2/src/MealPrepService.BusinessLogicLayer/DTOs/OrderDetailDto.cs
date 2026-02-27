namespace MealPrepService.BusinessLogicLayer.DTOs;

public class OrderDetailDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid MenuMealId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public MenuMealDto? MenuMeal { get; set; }

    // Helper properties for UI
    public string RecipeName => MenuMeal?.RecipeName ?? "Unknown";
    public decimal TotalPrice => UnitPrice * Quantity;
    public RecipeDto? Recipe => MenuMeal?.Recipe;
}