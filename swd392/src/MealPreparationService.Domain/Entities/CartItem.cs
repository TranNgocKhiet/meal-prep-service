namespace MealPreparationService.Domain.Entities;

public class CartItem : BaseEntity
{
    public string CartId { get; set; } = string.Empty;
    public Cart Cart { get; set; } = null!;
    public string MenuMealId { get; set; } = string.Empty;
    public MenuMeal MenuMeal { get; set; } = null!;
    public int Quantity { get; set; }
}
