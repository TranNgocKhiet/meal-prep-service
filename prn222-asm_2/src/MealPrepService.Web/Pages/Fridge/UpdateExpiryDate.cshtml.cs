using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;


namespace MealPrepService.Web.Pages.Fridge;

[Authorize(Roles = "Customer")]
public class UpdateExpiryDateModel : PageModel
{
    private readonly IFridgeService _fridgeService;
    private readonly ILogger<UpdateExpiryDateModel> _logger;

    [BindProperty]
    public Guid FridgeItemId { get; set; }

    [BindProperty]
    public DateTime NewExpiryDate { get; set; }

    [BindProperty]
    public string IngredientName { get; set; } = string.Empty;

    public UpdateExpiryDateModel(
        IFridgeService fridgeService,
        ILogger<UpdateExpiryDateModel> logger)
    {
        _fridgeService = fridgeService ?? throw new ArgumentNullException(nameof(fridgeService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Invalid expiry date value.";
            return RedirectToPage("/Fridge/Index");
        }

        try
        {
            await _fridgeService.UpdateExpiryDateAsync(FridgeItemId, NewExpiryDate);
            
            _logger.LogInformation("Fridge item {FridgeItemId} expiry date updated to {NewExpiryDate} for account {AccountId}", 
                FridgeItemId, NewExpiryDate, GetCurrentAccountId());
            
            TempData["SuccessMessage"] = $"{IngredientName} expiry date updated successfully! Duplicate items with the same expiry date have been merged.";
            return RedirectToPage("/Fridge/Index");
        }
        catch (BusinessException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToPage("/Fridge/Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating fridge item expiry date {FridgeItemId} for account {AccountId}", 
                FridgeItemId, GetCurrentAccountId());
            TempData["ErrorMessage"] = "An error occurred while updating the expiry date. Please try again.";
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
