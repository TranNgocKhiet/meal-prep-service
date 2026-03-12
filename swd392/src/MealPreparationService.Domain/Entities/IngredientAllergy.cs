namespace MealPreparationService.Domain.Entities;

public class IngredientAllergy : BaseEntity
{
    public string IngredientId { get; set; } = string.Empty;
    public Ingredient Ingredient { get; set; } = null!;
    public string AllergyId { get; set; } = string.Empty;
    public Allergy Allergy { get; set; } = null!;
}
