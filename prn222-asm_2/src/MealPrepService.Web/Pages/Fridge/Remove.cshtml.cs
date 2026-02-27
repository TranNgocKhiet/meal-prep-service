using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;


namespace MealPrepService.Web.Pages.Fridge;

[Authorize(Roles = "Customer")]
public class RemoveModel : PageModel
{
    private readonly IFridgeService _fridgeService;
    private readonly ILogger<RemoveModel> _logger;

    public RemoveModel(
        IFridgeService fridgeService,
        ILogger<RemoveModel> logger)
    {
        _fridgeService = fridgeService ?? throw new ArgumentNullException(nameof(fridgeService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IActionResult> OnPostAsync(Guid id)
    {
        try
        {
            await _fridgeService.RemoveItemAsync(id);
            
            _logger.LogInformation("Fridge item {FridgeItemId} removed successfully for account {AccountId}", 
                id, GetCurrentAccountId());
            
            TempData["SuccessMessage"] = "Item removed from your fridge successfully!";
            return RedirectToPage("/Fridge/Index");
        }
        catch (BusinessException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToPage("/Fridge/Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while removing fridge item {FridgeItemId} for account {AccountId}", 
                id, GetCurrentAccountId());
            TempData["ErrorMessage"] = "An error occurred while removing the item. Please try again.";
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
