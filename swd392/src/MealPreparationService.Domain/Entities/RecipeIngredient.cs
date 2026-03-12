namespace MealPreparationService.Domain.Entities;

public class RecipeIngredient : BaseEntity
{
    public string RecipeId { get; set; } = string.Empty;
    public Recipe Recipe { get; set; } = null!;
    public string IngredientId { get; set; } = string.Empty;
    public Ingredient Ingredient { get; set; } = null!;
    public decimal Amount { get; set; }
}
