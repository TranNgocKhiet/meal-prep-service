namespace MealPreparationService.Domain.Entities;

public class Feedback : BaseEntity
{
    public string CustomerId { get; set; } = string.Empty;
    public Account Customer { get; set; } = null!;
    
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
