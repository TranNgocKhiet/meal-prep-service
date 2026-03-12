namespace MealPreparationService.Domain.Entities;

public class Nutrient : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    
    // Navigation properties
    public ICollection<IngredientNutrient> IngredientNutrients { get; set; } = new List<IngredientNutrient>();
}
