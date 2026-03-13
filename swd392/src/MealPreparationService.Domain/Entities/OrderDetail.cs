namespace MealPreparationService.Domain.Entities;

public class OrderDetail : BaseEntity
{
    public string OrderId { get; set; } = string.Empty;
    public Order Order { get; set; } = null!;
    public string MenuMealId { get; set; } = string.Empty;
    public MenuMeal MenuMeal { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}



