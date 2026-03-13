namespace MealPreparationService.Business.DTOs;

public class SubscriptionPackageDto
{
    public string Id { get; set; } = string.Empty;
    public string PackageName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int CreditAmount { get; set; }
    public int DurationDays { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateSubscriptionPackageDto
{
    public string PackageName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int CreditAmount { get; set; }
    public int DurationDays { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class UpdateSubscriptionPackageDto
{
    public string PackageName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int CreditAmount { get; set; }
    public int DurationDays { get; set; }
    public string Description { get; set; } = string.Empty;
}
