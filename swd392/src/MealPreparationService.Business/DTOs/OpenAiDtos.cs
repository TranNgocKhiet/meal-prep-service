namespace MealPreparationService.Business.DTOs;

/// <summary>
/// Prompt data for AI meal plan generation
/// </summary>
public class AiMealPlanPromptDto
{
    public string HealthInformation { get; set; } = string.Empty;
    public string Goals { get; set; } = string.Empty;
    public List<FridgeItemDto> FridgeContents { get; set; } = new();
    public List<RecipeDto> AvailableRecipes { get; set; } = new();
    public int DurationDays { get; set; }
    public List<string> UserAllergens { get; set; } = new();
}

/// <summary>
/// Response from AI meal plan generation
/// </summary>
public class AiMealPlanResponseDto
{
    public List<AiMealDayDto> Days { get; set; } = new();
    public string Reasoning { get; set; } = string.Empty;
}

/// <summary>
/// AI-suggested meal for a specific day
/// </summary>
public class AiMealDayDto
{
    public int DayNumber { get; set; }
    public List<AiMealDto> Meals { get; set; } = new();
}

/// <summary>
/// AI-suggested meal with recipe IDs
/// </summary>
public class AiMealDto
{
    public int MealTypeId { get; set; }
    public List<string> RecipeIds { get; set; } = new();
}

/// <summary>
/// Prompt data for nutrient calculation
/// </summary>
public class NutrientPromptDto
{
    public List<IngredientPortionDto> Ingredients { get; set; } = new();
}

/// <summary>
/// Ingredient with portion information
/// </summary>
public class IngredientPortionDto
{
    public string IngredientId { get; set; } = string.Empty;
    public string IngredientName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
}

/// <summary>
/// Nutritional data calculated by AI
/// </summary>
public class NutrientDataDto
{
    public decimal TotalCalories { get; set; }
    public decimal TotalProteins { get; set; }
    public decimal TotalCarbohydrates { get; set; }
    public decimal TotalFats { get; set; }
    public Dictionary<string, decimal> Vitamins { get; set; } = new();
    public Dictionary<string, decimal> Minerals { get; set; } = new();
    public decimal CaloriesPerServing { get; set; }
    public int Servings { get; set; }
}

/// <summary>
/// Request for AI-generated meal plan
/// </summary>
public class AiMealPlanRequestDto
{
    public string Name { get; set; } = string.Empty;
    public int DurationDays { get; set; }
    public DateTime StartDate { get; set; }
    public string HealthInformation { get; set; } = string.Empty;
    public string Goals { get; set; } = string.Empty;
}
