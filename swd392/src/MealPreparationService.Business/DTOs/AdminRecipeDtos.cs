using System.ComponentModel.DataAnnotations;

namespace MealPreparationService.Business.DTOs;

public class AdminRecipeDto
{
    public string? Id { get; set; }
    
    [Required(ErrorMessage = "Recipe name is required")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Recipe name must be between 3 and 200 characters")]
    public string RecipeName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Instructions are required")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "Instructions must be between 10 and 2000 characters")]
    public string Instructions { get; set; } = string.Empty;
    
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateRecipeDto
{
    [Required(ErrorMessage = "Recipe name is required")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Recipe name must be between 3 and 200 characters")]
    public string RecipeName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Instructions are required")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "Instructions must be between 10 and 2000 characters")]
    public string Instructions { get; set; } = string.Empty;
}

public class UpdateRecipeDto
{
    [Required(ErrorMessage = "Recipe name is required")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Recipe name must be between 3 and 200 characters")]
    public string RecipeName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Instructions are required")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "Instructions must be between 10 and 2000 characters")]
    public string Instructions { get; set; } = string.Empty;
}

public class AdminRecipeIngredientDto
{
    public string Id { get; set; } = string.Empty;
    public string IngredientId { get; set; } = string.Empty;
    public string IngredientName { get; set; } = string.Empty;
    public string IngredientUnit { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class CreateRecipeIngredientDto
{
    [Required(ErrorMessage = "Ingredient is required")]
    public string IngredientId { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }
}

public class UpdateRecipeIngredientDto
{
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }
}
