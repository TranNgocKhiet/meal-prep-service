using System.ComponentModel.DataAnnotations;

namespace MealPreparationService.Business.DTOs;

public class MealStatusDto
{
    public string Id { get; set; } = string.Empty;
    public int MealTypeId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<RecipeDto> Recipes { get; set; } = new();
}

public class MealPlanProgressDto
{
    public string MealPlanId { get; set; } = string.Empty;
    public string MealPlanName { get; set; } = string.Empty;
    public int TotalMeals { get; set; }
    public int FinishedMeals { get; set; }
    public int ExpiredMeals { get; set; }
    public int PendingMeals { get; set; }
    public bool IsCompleted { get; set; }
    public decimal CompletionPercentage { get; set; }
}

public class MealIngredientCheckDto
{
    public string IngredientId { get; set; } = string.Empty;
    public string IngredientName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal RequiredAmount { get; set; }
    public decimal AvailableAmount { get; set; }
    public decimal MissingAmount { get; set; }
    public bool IsAvailable { get; set; }
}

public class MealFinishCheckDto
{
    public string MealId { get; set; } = string.Empty;
    public List<MealIngredientCheckDto> Ingredients { get; set; } = new List<MealIngredientCheckDto>();
    public bool CanFinish { get; set; }
    public int TotalIngredients { get; set; }
    public int AvailableIngredients { get; set; }
    public int MissingIngredients { get; set; }
}

public class IngredientReturnDto
{
    public string IngredientId { get; set; } = string.Empty;
    public string IngredientName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime ExpiryDate { get; set; }
}

public class MealUnfinishCheckDto
{
    public string MealId { get; set; } = string.Empty;
    public List<IngredientReturnDto> Ingredients { get; set; } = new List<IngredientReturnDto>();
    public int TotalIngredients { get; set; }
}

public class UnfinishMealDto
{
    [Required]
    public List<IngredientReturnDto> Ingredients { get; set; } = new List<IngredientReturnDto>();
}
