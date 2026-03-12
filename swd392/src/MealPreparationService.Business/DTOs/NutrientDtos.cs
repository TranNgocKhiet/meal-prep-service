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
