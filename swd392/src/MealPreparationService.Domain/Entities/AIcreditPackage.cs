namespace MealPreparationService.Domain.Entities;

public class AIcreditPackage : BaseEntity
{
    public string PackageName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int CreditAmount { get; set; }
    
    // Navigation properties
    public ICollection<AIcreditTransaction> AIcreditTransactions { get; set; } = new List<AIcreditTransaction>();
}
