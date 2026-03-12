using MealPreparationService.Business.DTOs;

namespace MealPreparationService.Business.Services;

public interface ICartService
{
    Task<CartDto> GetCartAsync(string userId);
    Task<CartDto> AddItemToCartAsync(string userId, AddCartItemDto dto);
    Task<CartDto> UpdateCartItemAsync(string userId, string cartItemId, int quantity);
    Task RemoveCartItemAsync(string userId, string cartItemId);
    Task ClearCartAsync(string userId);
}
