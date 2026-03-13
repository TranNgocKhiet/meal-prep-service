namespace MealPreparationService.Domain.Entities;

public class Recipe : BaseEntity
{
    public string RecipeName { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();
    public ICollection<MealRecipe> MealRecipes { get; set; } = new List<MealRecipe>();
    public ICollection<MenuMealRecipe> MenuMealRecipes { get; set; } = new List<MenuMealRecipe>();
}




