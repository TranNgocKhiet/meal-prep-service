namespace MealPreparationService.Domain.Entities;

public class Cart : BaseEntity
{
    public string AccountId { get; set; } = string.Empty;
    public Account Account { get; set; } = null!;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
}
