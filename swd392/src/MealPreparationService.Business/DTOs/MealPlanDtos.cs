using System.ComponentModel.DataAnnotations;

namespace MealPreparationService.Business.DTOs;

public class CreateMealPlanDto
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 100 characters")]
    public string Name { get; set; } = string.Empty;
    
    [Range(1, 7, ErrorMessage = "Duration must be between 1 and 7 days")]
    public int? DurationDays { get; set; }
    
    public DateTime? StartDate { get; set; }
    
    // Personal Information
    [Range(1, 120, ErrorMessage = "Age must be between 1 and 120")]
    public int? Age { get; set; }
    
    [Range(1, 500, ErrorMessage = "Weight must be between 1 and 500 kg")]
    public decimal? Weight { get; set; }
    
    [Range(1, 300, ErrorMessage = "Height must be between 1 and 300 cm")]
    public decimal? Height { get; set; }
    
    [StringLength(20, ErrorMessage = "Gender must not exceed 20 characters")]
    public string? Gender { get; set; }
    
    [StringLength(1000, ErrorMessage = "Health note must not exceed 1000 characters")]
    public string? HealthNote { get; set; }
    
    [Range(500, 10000, ErrorMessage = "Calories goal must be between 500 and 10000")]
    public int? CaloriesGoal { get; set; }
    
    // Health Profile data
    public List<string>? Allergies { get; set; }
    public List<string>? LikedIngredients { get; set; }
    public List<string>? DislikedIngredients { get; set; }
    public List<string>? AllergyIngredients { get; set; }
}

public class UpdateMealPlanDto
{
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 100 characters")]
    public string? Name { get; set; }
    
    public List<MealPlanDayDto>? Days { get; set; }
}

public class MealPlanDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int DurationDays { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsAiGenerated { get; set; }
    public bool IsActive { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<MealPlanDayDto> Days { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    
    // Personal Information
    public int? Age { get; set; }
    public decimal? Weight { get; set; }
    public decimal? Height { get; set; }
    public string? Gender { get; set; }
    public string? HealthNote { get; set; }
    public int? CaloriesGoal { get; set; }
}

public class MealPlanDayDto
{
    public string? Id { get; set; }
    
    [Required(ErrorMessage = "Day number is required")]
    [Range(1, 7, ErrorMessage = "Day number must be between 1 and 7")]
    public int DayNumber { get; set; }
    
    [Required(ErrorMessage = "Date is required")]
    public DateTime Date { get; set; }
    
    [Required(ErrorMessage = "Meals are required")]
    public List<MealDto> Meals { get; set; } = new();
}

public class MealDto
{
    public string? Id { get; set; }
    
    [Required(ErrorMessage = "Meal type is required")]
    [Range(1, 3, ErrorMessage = "Meal type must be 1 (Breakfast), 2 (Lunch), or 3 (Dinner)")]
    public int MealTypeId { get; set; }
    
    [Required(ErrorMessage = "Recipe IDs are required")]
    [MaxLength(10, ErrorMessage = "Maximum 10 recipes per meal")]
    public List<string> RecipeIds { get; set; } = new();
    
    public List<RecipeDto>? Recipes { get; set; }
    public string? Status { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? MealPlanId { get; set; }
    public DateTime? Date { get; set; }
    
    // Nutrition information
    public decimal TotalCalories { get; set; }
    public decimal ProteinG { get; set; }
    public decimal FatG { get; set; }
    public decimal CarbsG { get; set; }
}

public class RecipeDto
{
    public string Id { get; set; } = string.Empty;
    public string RecipeName { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int PreparationTimeMinutes { get; set; }
    public string DifficultyLevel { get; set; } = string.Empty;
    public int Servings { get; set; }
    public List<RecipeIngredientDto>? Ingredients { get; set; }
    public bool HasAllergyWarning { get; set; }
    public List<string>? Allergens { get; set; }
    public bool IsFavorite { get; set; }
    
    // Nutrition information per serving
    public decimal TotalCalories { get; set; }
    public decimal ProteinG { get; set; }
    public decimal FatG { get; set; }
    public decimal CarbsG { get; set; }
}

public class RecipeIngredientDto
{
    public string IngredientId { get; set; } = string.Empty;
    public string IngredientName { get; set; } = string.Empty;
    public IngredientDto? Ingredient { get; set; }
    public decimal Amount { get; set; }
}

public class RecipeSearchDto
{
    public string? SearchTerm { get; set; }
    public string? Category { get; set; }
    public int? MaxPreparationTime { get; set; }
    public string? DifficultyLevel { get; set; }
    public bool ExcludeAllergens { get; set; } = true;
}

public class IngredientSearchDto
{
    public string? SearchTerm { get; set; }
    public string? Category { get; set; }
}

public class AllergyDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
}
