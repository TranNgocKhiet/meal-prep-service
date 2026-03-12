using System.ComponentModel.DataAnnotations;

namespace MealPreparationService.Business.DTOs;

public class CartDto
{
    public string Id { get; set; } = string.Empty;
    public List<CartItemDto> CartItems { get; set; } = new();
    public DateTime UpdatedAt { get; set; }
}

public class CartItemDto
{
    public string Id { get; set; } = string.Empty;
    public string MenuMealId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public MenuMealDto MenuMeal { get; set; } = null!;
}

public class MenuMealDto
{
    public string Id { get; set; } = string.Empty;
    public int MealTypeId { get; set; }
    public decimal Price { get; set; }
    public int AvailableQuantity { get; set; }
    public List<MenuMealRecipeDto> MenuMealRecipes { get; set; } = new();
}

public class MenuMealRecipeDto
{
    public RecipeDto Recipe { get; set; } = null!;
}

public class AddCartItemDto
{
    [Required(ErrorMessage = "Menu meal ID is required")]
    public string MenuMealId { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Quantity is required")]
    [Range(1, 100, ErrorMessage = "Quantity must be between 1 and 100")]
    public int Quantity { get; set; } = 1;
}

public class UpdateCartItemDto
{
    [Required(ErrorMessage = "Quantity is required")]
    [Range(1, 100, ErrorMessage = "Quantity must be between 1 and 100")]
    public int Quantity { get; set; }
}
