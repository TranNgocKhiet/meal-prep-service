namespace MealPreparationService.Business.DTOs;

/// <summary>
/// Request for nutrient calculation
/// </summary>
public class NutrientRequestDto
{
    public List<IngredientPortionDto> Ingredients { get; set; } = new();
    public string? CalculationName { get; set; }
}

/// <summary>
/// Complete nutrient calculation result
/// </summary>
public class NutrientCalculationDto
{
    public string Id { get; set; } = string.Empty;
    public string? CalculationName { get; set; }
    public List<IngredientPortionDto> Ingredients { get; set; } = new();
    public NutrientDataDto NutrientData { get; set; } = new();
    public string HealthAdvice { get; set; } = string.Empty;
    public DateTime CalculatedAt { get; set; }
    public bool IsSaved { get; set; }
}

public class CustomMealNutritionRequestDto
{
    public string MealDescription { get; set; } = string.Empty;
    public List<CustomMealIngredientDto> Ingredients { get; set; } = new();
}

public class CustomMealIngredientDto
{
    public string IngredientName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
}

public class CustomMealNutritionResponseDto
{
    public string MealSummary { get; set; } = string.Empty;
    public decimal TotalCalories { get; set; }
    public decimal ProteinG { get; set; }
    public decimal CarbsG { get; set; }
    public decimal FatG { get; set; }
    public decimal FiberG { get; set; }
    public decimal SugarG { get; set; }
    public decimal SodiumMg { get; set; }
    public List<string> IngredientConflicts { get; set; } = new();
    public string BestConsumptionAdvice { get; set; } = string.Empty;
    public string OverallNote { get; set; } = string.Empty;
}
