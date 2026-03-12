using MealPreparationService.Domain.Entities;

namespace MealPreparationService.DataAccess.Repositories;

/// <summary>
/// Repository interface for FridgeItem entity operations.
/// </summary>
public interface IFridgeItemRepository : IRepository<FridgeItem>
{
    /// <summary>
    /// Gets all fridge items for a specific fridge.
    /// </summary>
    /// <param name="fridgeId">The fridge ID to filter by.</param>
    /// <returns>A list of fridge items belonging to the specified fridge.</returns>
    Task<List<FridgeItem>> GetByFridgeIdAsync(string fridgeId);

    /// <summary>
    /// Gets all fridge items for a specific account.
    /// </summary>
    /// <param name="accountId">The account ID to filter by.</param>
    /// <returns>A list of fridge items belonging to the specified account.</returns>
    Task<List<FridgeItem>> GetByAccountIdAsync(string accountId);

    /// <summary>
    /// Gets all fridge items expiring before a specified date.
    /// </summary>
    /// <param name="fridgeId">The fridge ID to filter by.</param>
    /// <param name="beforeDate">The date to filter items expiring before.</param>
    /// <returns>A list of fridge items expiring before the specified date.</returns>
    Task<List<FridgeItem>> GetExpiringItemsAsync(string fridgeId, DateTime beforeDate);

    /// <summary>
    /// Gets a fridge item by ID with its ingredient.
    /// </summary>
    /// <param name="id">The fridge item ID.</param>
    /// <returns>The fridge item with ingredient, or null if not found.</returns>
    Task<FridgeItem?> GetByIdWithIngredientAsync(string id);
}
