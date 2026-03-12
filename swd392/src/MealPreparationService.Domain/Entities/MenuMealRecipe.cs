namespace MealPreparationService.Domain.Entities;

public class MenuMealRecipe
{
    public string MenuMealId { get; set; } = string.Empty;
    public MenuMeal MenuMeal { get; set; } = null!;
    public string RecipeId { get; set; } = string.Empty;
    public Recipe Recipe { get; set; } = null!;
}
