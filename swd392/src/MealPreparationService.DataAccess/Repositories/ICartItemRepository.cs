using MealPreparationService.Domain.Entities;

namespace MealPreparationService.DataAccess.Repositories;

/// <summary>
/// Repository interface for CartItem entity operations.
/// </summary>
public interface ICartItemRepository : IRepository<CartItem>
{
    /// <summary>
    /// Gets all cart items for a specific cart.
    /// </summary>
    /// <param name="cartId">The cart ID to filter by.</param>
    /// <returns>A list of cart items belonging to the specified cart.</returns>
    Task<List<CartItem>> GetByCartIdAsync(string cartId);

    /// <summary>
    /// Gets a specific cart item by cart and menu meal.
    /// </summary>
    /// <param name="cartId">The cart ID to filter by.</param>
    /// <param name="menuMealId">The menu meal ID to filter by.</param>
    /// <returns>The cart item matching the cart and menu meal, or null if not found.</returns>
    Task<CartItem?> GetByCartAndMenuMealAsync(string cartId, string menuMealId);
}
