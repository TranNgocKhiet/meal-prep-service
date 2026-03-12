namespace MealPreparationService.Domain.Entities;

public class RelationshipType
{
    public int Id { get; set; }
    public string TypeName { get; set; } = string.Empty;
    
    // Navigation properties
    public ICollection<HealthProfileIngredient> HealthProfileIngredients { get; set; } = new List<HealthProfileIngredient>();
}
