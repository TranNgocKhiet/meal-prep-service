using MealPreparationService.DataAccess.Data;
using MealPreparationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MealPreparationService.DataAccess.Repositories;

/// <summary>
/// Repository implementation for CartItem entity operations.
/// </summary>
public class CartItemRepository : Repository<CartItem>, ICartItemRepository
{
    public CartItemRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Gets a cart item by ID with related entities (Cart, MenuMeal).
    /// </summary>
    public override async Task<CartItem?> GetByIdAsync(string id)
    {
        return await _dbSet
            .Include(ci => ci.Cart)
            .Include(ci => ci.MenuMeal)
            .FirstOrDefaultAsync(ci => ci.Id == id);
    }

    /// <summary>
    /// Gets all cart items for a specific cart.
    /// </summary>
    public async Task<List<CartItem>> GetByCartIdAsync(string cartId)
    {
        return await _dbSet
            .Include(ci => ci.MenuMeal)
            .Where(ci => ci.CartId == cartId)
            .ToListAsync();
    }

    /// <summary>
    /// Gets a specific cart item by cart and menu meal.
    /// </summary>
    public async Task<CartItem?> GetByCartAndMenuMealAsync(string cartId, string menuMealId)
    {
        return await _dbSet
            .Include(ci => ci.MenuMeal)
            .Include(ci => ci.Cart)
            .FirstOrDefaultAsync(ci => ci.CartId == cartId && ci.MenuMealId == menuMealId);
    }
}
