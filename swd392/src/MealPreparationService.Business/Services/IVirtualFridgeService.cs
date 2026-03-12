using MealPreparationService.Business.DTOs;

namespace MealPreparationService.Business.Services;

public interface IVirtualFridgeService
{
    Task<FridgeItemDto> AddItemAsync(AddFridgeItemDto dto, string userId);
    Task<FridgeItemDto> UpdateItemAsync(string itemId, UpdateFridgeItemDto dto);
    Task DeleteItemAsync(string itemId);
    Task<List<FridgeItemDto>> GetUserFridgeItemsAsync(string userId, bool includeExpired = true);
    Task<bool> HasSufficientQuantityAsync(string userId, string ingredientId, decimal quantity);
    Task DeductIngredientsAsync(string userId, List<IngredientQuantityDto> ingredients);
    Task<List<FridgeItemDto>> GetExpiringItemsAsync(string userId, int daysThreshold);
    Task<GroceryListDto> GenerateGroceryListAsync(string userId);
    Task<List<FridgeItemDto>> PurchaseGroceryItemsAsync(string userId, PurchaseGroceryListDto dto);
}
