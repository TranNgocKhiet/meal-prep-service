using MealPreparationService.Business.DTOs;
using MealPreparationService.DataAccess.UnitOfWork;
using MealPreparationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MealPreparationService.Business.Services;

public class CartService : ICartService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CartService> _logger;

    public CartService(IUnitOfWork unitOfWork, ILogger<CartService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CartDto> GetCartAsync(string userId)
    {
        _logger.LogInformation("Getting cart for user {UserId}", userId);

        var cart = await _unitOfWork.Carts.GetAllQueryable()
            .Include(c => c.CartItems)
                .ThenInclude(ci => ci.MenuMeal)
                    .ThenInclude(mm => mm.MenuMealRecipes)
                        .ThenInclude(mmr => mmr.Recipe)
            .FirstOrDefaultAsync(c => c.AccountId == userId);

        if (cart == null)
        {
            // Create a new cart if it doesn't exist
            cart = new Cart
            {
                Id = Guid.NewGuid().ToString(),
                AccountId = userId,
                UpdatedAt = DateTime.UtcNow
            };
            
            await _unitOfWork.Carts.AddAsync(cart);
            await _unitOfWork.SaveChangesAsync();
        }

        return MapToCartDto(cart);
    }

    public async Task<CartDto> AddItemToCartAsync(string userId, AddCartItemDto dto)
    {
        _logger.LogInformation("Adding item to cart for user {UserId}", userId);

        // Get or create cart
        var cart = await _unitOfWork.Carts.GetAllQueryable()
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.AccountId == userId);

        if (cart == null)
        {
            cart = new Cart
            {
                Id = Guid.NewGuid().ToString(),
                AccountId = userId,
                UpdatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Carts.AddAsync(cart);
        }

        // Check if menu meal exists and has availability
        var menuMeal = await _unitOfWork.MenuMeals.GetAllQueryable()
            .Include(mm => mm.Menu)
            .FirstOrDefaultAsync(mm => mm.Id == dto.MenuMealId);

        if (menuMeal == null)
        {
            throw new KeyNotFoundException($"Menu meal {dto.MenuMealId} not found");
        }

        // Check if menu date is in the past
        var menuDate = menuMeal.Menu.MenuDate.Date;
        var today = DateTime.UtcNow.Date;
        
        if (menuDate < today)
        {
            throw new InvalidOperationException("Cannot order meals from past menus");
        }

        if (menuMeal.AvailableQuantity < dto.Quantity)
        {
            throw new InvalidOperationException($"Only {menuMeal.AvailableQuantity} items available");
        }

        // Check if item already exists in cart
        var existingItem = cart.CartItems.FirstOrDefault(ci => ci.MenuMealId == dto.MenuMealId);

        if (existingItem != null)
        {
            // Update quantity
            var newQuantity = existingItem.Quantity + dto.Quantity;
            if (newQuantity > menuMeal.AvailableQuantity)
            {
                throw new InvalidOperationException($"Cannot add more items. Only {menuMeal.AvailableQuantity} available");
            }
            existingItem.Quantity = newQuantity;
        }
        else
        {
            // Add new item
            var cartItem = new CartItem
            {
                Id = Guid.NewGuid().ToString(),
                CartId = cart.Id,
                MenuMealId = dto.MenuMealId,
                Quantity = dto.Quantity
            };
            cart.CartItems.Add(cartItem);
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();

        return await GetCartAsync(userId);
    }

    public async Task<CartDto> UpdateCartItemAsync(string userId, string cartItemId, int quantity)
    {
        _logger.LogInformation("Updating cart item {CartItemId} for user {UserId}", cartItemId, userId);

        var cart = await _unitOfWork.Carts.GetAllQueryable()
            .Include(c => c.CartItems)
                .ThenInclude(ci => ci.MenuMeal)
            .FirstOrDefaultAsync(c => c.AccountId == userId);

        if (cart == null)
        {
            throw new KeyNotFoundException("Cart not found");
        }

        var cartItem = cart.CartItems.FirstOrDefault(ci => ci.Id == cartItemId);

        if (cartItem == null)
        {
            throw new KeyNotFoundException($"Cart item {cartItemId} not found");
        }

        if (quantity > cartItem.MenuMeal.AvailableQuantity)
        {
            throw new InvalidOperationException($"Only {cartItem.MenuMeal.AvailableQuantity} items available");
        }

        cartItem.Quantity = quantity;
        cart.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();

        return await GetCartAsync(userId);
    }

    public async Task RemoveCartItemAsync(string userId, string cartItemId)
    {
        _logger.LogInformation("Removing cart item {CartItemId} for user {UserId}", cartItemId, userId);

        var cart = await _unitOfWork.Carts.GetAllQueryable()
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.AccountId == userId);

        if (cart == null)
        {
            throw new KeyNotFoundException("Cart not found");
        }

        var cartItem = cart.CartItems.FirstOrDefault(ci => ci.Id == cartItemId);

        if (cartItem == null)
        {
            throw new KeyNotFoundException($"Cart item {cartItemId} not found");
        }

        cart.CartItems.Remove(cartItem);
        cart.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task ClearCartAsync(string userId)
    {
        _logger.LogInformation("Clearing cart for user {UserId}", userId);

        var cart = await _unitOfWork.Carts.GetAllQueryable()
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.AccountId == userId);

        if (cart == null)
        {
            return;
        }

        cart.CartItems.Clear();
        cart.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();
    }

    private CartDto MapToCartDto(Cart cart)
    {
        return new CartDto
        {
            Id = cart.Id,
            UpdatedAt = cart.UpdatedAt,
            CartItems = cart.CartItems.Select(ci => new CartItemDto
            {
                Id = ci.Id,
                MenuMealId = ci.MenuMealId,
                Quantity = ci.Quantity,
                MenuMeal = new MenuMealDto
                {
                    Id = ci.MenuMeal.Id,
                    MealTypeId = ci.MenuMeal.MealTypeId,
                    Price = ci.MenuMeal.Price,
                    AvailableQuantity = ci.MenuMeal.AvailableQuantity,
                    MenuMealRecipes = ci.MenuMeal.MenuMealRecipes.Select(mmr => new MenuMealRecipeDto
                    {
                        Recipe = new RecipeDto
                        {
                            Id = mmr.Recipe.Id,
                            RecipeName = mmr.Recipe.RecipeName
                        }
                    }).ToList()
                }
            }).ToList()
        };
    }
}
