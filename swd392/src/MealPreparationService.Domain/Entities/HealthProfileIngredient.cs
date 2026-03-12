namespace MealPreparationService.Domain.Entities;

public class HealthProfileIngredient : BaseEntity
{
    public string HealthProfileId { get; set; } = string.Empty;
    public HealthProfile HealthProfile { get; set; } = null!;
    public string IngredientId { get; set; } = string.Empty;
    public Ingredient Ingredient { get; set; } = null!;
    public int RelationshipTypeId { get; set; }
    public RelationshipType RelationshipType { get; set; } = null!;
}
