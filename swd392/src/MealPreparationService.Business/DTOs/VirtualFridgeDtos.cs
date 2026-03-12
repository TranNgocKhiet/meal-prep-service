using System.ComponentModel.DataAnnotations;

namespace MealPreparationService.Business.DTOs;

public class AddFridgeItemDto
{
    [Required(ErrorMessage = "Ingredient ID is required")]
    public string IngredientId { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Quantity is required")]
    [Range(0.01, 10000, ErrorMessage = "Quantity must be between 0.01 and 10000")]
    public decimal Quantity { get; set; }
    
    [Required(ErrorMessage = "Expiry date is required")]
    public DateTime ExpiryDate { get; set; }
}

public class UpdateFridgeItemDto
{
    [Range(0.01, 10000, ErrorMessage = "Quantity must be between 0.01 and 10000")]
    public decimal? Quantity { get; set; }
    
    public DateTime? ExpiryDate { get; set; }
}

public class FridgeItemDto
{
    public string Id { get; set; } = string.Empty;
    public IngredientDto Ingredient { get; set; } = null!;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public bool IsExpired { get; set; }
    public int DaysUntilExpiry { get; set; }
    public DateTime AddedAt { get; set; }
}

public class IngredientDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal PricePerUnit { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsAvailableForPurchase { get; set; }
    public List<AllergyDto>? Allergies { get; set; }
}

public class IngredientQuantityDto
{
    public string IngredientId { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
}

public class GroceryListItemDto
{
    public string IngredientId { get; set; } = string.Empty;
    public string IngredientName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal RequiredQuantity { get; set; }
    public decimal CurrentQuantity { get; set; }
    public decimal MissingQuantity { get; set; }
    public decimal PricePerUnit { get; set; }
    public decimal EstimatedCost { get; set; }
    public bool IsSelected { get; set; } = true;
}

public class GroceryListDto
{
    public List<GroceryListItemDto> Items { get; set; } = new List<GroceryListItemDto>();
    public decimal TotalEstimatedCost { get; set; }
    public int TotalItems { get; set; }
}

public class PurchaseGroceryItemDto
{
    [Required(ErrorMessage = "Ingredient ID is required")]
    public string IngredientId { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Quantity is required")]
    [Range(0.01, 10000, ErrorMessage = "Quantity must be between 0.01 and 10000")]
    public decimal Quantity { get; set; }
    
    [Required(ErrorMessage = "Expiry date is required")]
    public DateTime ExpiryDate { get; set; }
}

public class PurchaseGroceryListDto
{
    [Required]
    public List<PurchaseGroceryItemDto> Items { get; set; } = new List<PurchaseGroceryItemDto>();
}
