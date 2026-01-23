namespace MealPrepService.DataAccessLayer.Entities;

public class FoodPreference : BaseEntity
{
    public string PreferenceName { get; set; } = string.Empty;
    
    // Link to ingredient for AI recommendations
    public Guid? IngredientId { get; set; }
    public Ingredient? Ingredient { get; set; }
    
    // Navigation properties
    public ICollection<HealthProfile> HealthProfiles { get; set; } = new List<HealthProfile>();
}
