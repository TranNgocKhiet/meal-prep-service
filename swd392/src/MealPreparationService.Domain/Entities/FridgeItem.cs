namespace MealPreparationService.Domain.Entities;

public class FridgeItem : BaseEntity
{
    public string FridgeId { get; set; } = string.Empty;
    public Fridge Fridge { get; set; } = null!;
    public string AccountId { get; set; } = string.Empty;
    public Account Account { get; set; } = null!;
    public string IngredientId { get; set; } = string.Empty;
    public Ingredient Ingredient { get; set; } = null!;
    public decimal CurrentAmount { get; set; }
    public DateTime ExpiryDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
