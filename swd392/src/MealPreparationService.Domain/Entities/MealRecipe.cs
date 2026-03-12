namespace MealPreparationService.Domain.Entities;

public class MealRecipe : BaseEntity
{
    public string MealId { get; set; } = string.Empty;
    public Meal Meal { get; set; } = null!;
    public string RecipeId { get; set; } = string.Empty;
    public Recipe Recipe { get; set; } = null!;
}

