namespace MealPreparationService.Domain.Entities;

public class Allergy : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    
    // Navigation properties
    public ICollection<IngredientAllergy> IngredientAllergies { get; set; } = new List<IngredientAllergy>();
    public ICollection<HealthProfileAllergy> HealthProfileAllergies { get; set; } = new List<HealthProfileAllergy>();
}
