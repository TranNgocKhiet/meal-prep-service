using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;


namespace MealPrepService.Web.Pages.Fridge;

[Authorize(Roles = "Customer")]
public class UpdateItemModel : PageModel
{
    private readonly IFridgeService _fridgeService;
    private readonly ILogger<UpdateItemModel> _logger;

    [BindProperty]
    public Guid FridgeItemId { get; set; }

    [BindProperty]
    public float NewAmount { get; set; }

    [BindProperty]
    public DateTime NewExpiryDate { get; set; }

    [BindProperty]
    public string IngredientName { get; set; } = string.Empty;

    public UpdateItemModel(
        IFridgeService fridgeService,
        ILogger<UpdateItemModel> logger)
    {
        _fridgeService = fridgeService ?? throw new ArgumentNullException(nameof(fridgeService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Invalid input values.";
            return RedirectToPage("/Fridge/Index");
        }

        try
        {
            var accountId = GetCurrentAccountId();
            var fridgeItem = await _fridgeService.GetFridgeItemsAsync(accountId);
            var item = fridgeItem.FirstOrDefault(f => f.Id == FridgeItemId);
            
            if (item == null)
            {
                TempData["ErrorMessage"] = "Item not found.";
                return RedirectToPage("/Fridge/Index");
            }

            bool quantityChanged = Math.Abs(item.CurrentAmount - NewAmount) > 0.001f;
            bool expiryChanged = item.ExpiryDate.Date != NewExpiryDate.Date;

            if (!quantityChanged && !expiryChanged)
            {
                TempData["InfoMessage"] = "No changes were made.";
                return RedirectToPage("/Fridge/Index");
            }

            // Update quantity if changed
            if (quantityChanged)
            {
                await _fridgeService.UpdateItemQuantityAsync(FridgeItemId, NewAmount);
                _logger.LogInformation("Fridge item {FridgeItemId} quantity updated from {OldAmount} to {NewAmount} for account {AccountId}", 
                    FridgeItemId, item.CurrentAmount, NewAmount, accountId);
            }

            // Update expiry date if changed
            if (expiryChanged)
            {
                await _fridgeService.UpdateExpiryDateAsync(FridgeItemId, NewExpiryDate);
                _logger.LogInformation("Fridge item {FridgeItemId} expiry date updated from {OldDate} to {NewDate} for account {AccountId}", 
                    FridgeItemId, item.ExpiryDate, NewExpiryDate, accountId);
            }

            // Build success message
            var changes = new List<string>();
            if (quantityChanged) changes.Add("quantity");
            if (expiryChanged) changes.Add("expiry date");
            
            var changeText = string.Join(" and ", changes);
            TempData["SuccessMessage"] = $"{IngredientName} {changeText} updated successfully!" + 
                (expiryChanged ? " Items with matching expiry dates have been merged." : "");
            
            return RedirectToPage("/Fridge/Index");
        }
        catch (BusinessException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToPage("/Fridge/Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating fridge item {FridgeItemId} for account {AccountId}", 
                FridgeItemId, GetCurrentAccountId());
            TempData["ErrorMessage"] = "An error occurred while updating the item. Please try again.";
            return RedirectToPage("/Fridge/Index");
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
}
