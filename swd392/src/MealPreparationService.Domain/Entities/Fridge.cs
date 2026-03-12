namespace MealPreparationService.Domain.Entities;

public class Fridge : BaseEntity
{
    public string AccountId { get; set; } = string.Empty;
    public Account Account { get; set; } = null!;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ICollection<FridgeItem> FridgeItems { get; set; } = new List<FridgeItem>();
}
