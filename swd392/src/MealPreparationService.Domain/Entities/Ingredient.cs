namespace MealPreparationService.Domain.Entities;

public class Ingredient : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    
    // Navigation properties
    public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();
    public ICollection<IngredientNutrient> IngredientNutrients { get; set; } = new List<IngredientNutrient>();
    public ICollection<IngredientAllergy> IngredientAllergies { get; set; } = new List<IngredientAllergy>();
    public ICollection<HealthProfileIngredient> HealthProfileIngredients { get; set; } = new List<HealthProfileIngredient>();
}
