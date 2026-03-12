namespace MealPreparationService.Domain.Entities;

public class IngredientNutrient : BaseEntity
{
    public string IngredientId { get; set; } = string.Empty;
    public Ingredient Ingredient { get; set; } = null!;
    public string NutrientId { get; set; } = string.Empty;
    public Nutrient Nutrient { get; set; } = null!;
    public decimal AmountPer100 { get; set; }
}
