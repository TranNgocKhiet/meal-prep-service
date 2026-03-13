namespace MealPreparationService.Business.DTOs;

public class AICreditPackageDto
{
    public string Id { get; set; } = string.Empty;
    public string PackageName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int CreditAmount { get; set; }
}

public class CreateAICreditPackageDto
{
    public string PackageName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int CreditAmount { get; set; }
}

public class UpdateAICreditPackageDto
{
    public string PackageName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int CreditAmount { get; set; }
}
