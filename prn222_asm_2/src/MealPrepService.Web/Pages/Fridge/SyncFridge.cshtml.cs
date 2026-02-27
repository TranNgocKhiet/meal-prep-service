using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;


namespace MealPrepService.Web.Pages.Fridge;

[Authorize(Roles = "Customer")]
public class SyncFridgeModel : PageModel
{
    private readonly IFridgeService _fridgeService;
    private readonly IIngredientService _ingredientService;
    private readonly ILogger<SyncFridgeModel> _logger;

    [BindProperty]
    public Dictionary<Guid, PurchasedIngredientInput> PurchasedIngredients { get; set; } = new();

    public SyncFridgeModel(
        IFridgeService fridgeService, 
        IIngredientService ingredientService,
        ILogger<SyncFridgeModel> logger)
    {
        _fridgeService = fridgeService ?? throw new ArgumentNullException(nameof(fridgeService));
        _ingredientService = ingredientService ?? throw new ArgumentNullException(nameof(ingredientService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            var accountId = GetCurrentAccountId();
            
            if (PurchasedIngredients == null || !PurchasedIngredients.Any())
            {
                TempData["ErrorMessage"] = "No items were selected to purchase.";
                return RedirectToPage("/Fridge/GroceryList");
            }

            // Get the purchased items
            var purchasedItems = PurchasedIngredients.Values
                .Where(p => p.IsPurchased)
                .ToList();

            if (!purchasedItems.Any())
            {
                TempData["ErrorMessage"] = "No items were selected to purchase.";
                return RedirectToPage("/Fridge/GroceryList");
            }

            // Get current fridge items
            var fridgeItems = await _fridgeService.GetFridgeItemsAsync(accountId);
            var fridgeInventory = fridgeItems
                .GroupBy(f => new { f.IngredientId, ExpiryDate = f.ExpiryDate.Date })
                .ToDictionary(
                    g => g.Key, 
                    g => new { Item = g.First(), TotalAmount = g.Sum(f => f.CurrentAmount) }
                );

            int itemsAdded = 0;
            int itemsUpdated = 0;

            foreach (var purchasedItem in purchasedItems)
            {
                // Get ingredient details
                var ingredient = await _ingredientService.GetByIdAsync(purchasedItem.IngredientId);
                if (ingredient == null)
                {
                    _logger.LogWarning("Ingredient {IngredientId} not found while syncing fridge", purchasedItem.IngredientId);
                    continue;
                }

                var expiryDate = purchasedItem.ExpiryDate != default(DateTime) 
                    ? purchasedItem.ExpiryDate 
                    : DateTime.Today.AddDays(7);
                
                var inventoryKey = new { IngredientId = purchasedItem.IngredientId, ExpiryDate = expiryDate.Date };

                if (fridgeInventory.ContainsKey(inventoryKey))
                {
                    // Update existing fridge item
                    var existingData = fridgeInventory[inventoryKey];
                    var newAmount = existingData.TotalAmount + purchasedItem.Amount;
                    await _fridgeService.UpdateItemQuantityAsync(existingData.Item.Id, newAmount);
                    itemsUpdated++;
                    
                    _logger.LogInformation("Updated fridge item {IngredientName} (Expiry: {ExpiryDate}) from {OldAmount} to {NewAmount} for account {AccountId}",
                        ingredient.IngredientName, expiryDate.ToShortDateString(), existingData.TotalAmount, newAmount, accountId);
                }
                else
                {
                    // Add new fridge item
                    var fridgeItemDto = new FridgeItemDto
                    {
                        AccountId = accountId,
                        IngredientId = purchasedItem.IngredientId,
                        IngredientName = ingredient.IngredientName,
                        Unit = ingredient.Unit,
                        CurrentAmount = purchasedItem.Amount,
                        ExpiryDate = expiryDate
                    };
                    
                    await _fridgeService.AddItemAsync(fridgeItemDto);
                    itemsAdded++;
                    
                    _logger.LogInformation("Added new fridge item {IngredientName} with amount {Amount} and expiry date {ExpiryDate} for account {AccountId}",
                        ingredient.IngredientName, purchasedItem.Amount, expiryDate, accountId);
                }
            }

            var successMessage = $"Successfully updated your fridge! {itemsAdded} item(s) added";
            if (itemsUpdated > 0)
            {
                successMessage += $", {itemsUpdated} item(s) updated";
            }
            successMessage += ".";
            
            TempData["SuccessMessage"] = successMessage;
            
            _logger.LogInformation("Fridge sync completed for account {AccountId}: {ItemsAdded} added, {ItemsUpdated} updated", 
                accountId, itemsAdded, itemsUpdated);
            
            return RedirectToPage("/Fridge/Index");
        }
        catch (BusinessException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToPage("/Fridge/GroceryList");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while syncing fridge for account {AccountId}", GetCurrentAccountId());
            TempData["ErrorMessage"] = "An error occurred while updating your fridge. Please try again.";
            return RedirectToPage("/Fridge/GroceryList");
        }
    }

    private Guid GetCurrentAccountId()
    {
        var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(accountIdClaim) || !Guid.TryParse(accountIdClaim, out var accountId))
        {
            throw new AuthenticationException("User account ID not found in claims.");
        }
        return accountId;
    }

    public class PurchasedIngredientInput
    {
        public Guid IngredientId { get; set; }
        public float Amount { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool IsPurchased { get; set; }
    }
}
