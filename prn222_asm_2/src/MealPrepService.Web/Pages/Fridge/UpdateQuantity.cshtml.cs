using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;


namespace MealPrepService.Web.Pages.Fridge;

[Authorize(Roles = "Customer")]
public class UpdateQuantityModel : PageModel
{
    private readonly IFridgeService _fridgeService;
    private readonly ILogger<UpdateQuantityModel> _logger;

    [BindProperty]
    public Guid FridgeItemId { get; set; }

    [BindProperty]
    public float NewAmount { get; set; }

    [BindProperty]
    public string IngredientName { get; set; } = string.Empty;

    public UpdateQuantityModel(
        IFridgeService fridgeService,
        ILogger<UpdateQuantityModel> logger)
    {
        _fridgeService = fridgeService ?? throw new ArgumentNullException(nameof(fridgeService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Invalid quantity value.";
            return RedirectToPage("/Fridge/Index");
        }

        try
        {
            await _fridgeService.UpdateItemQuantityAsync(FridgeItemId, NewAmount);
            
            _logger.LogInformation("Fridge item {FridgeItemId} quantity updated to {NewAmount} for account {AccountId}", 
                FridgeItemId, NewAmount, GetCurrentAccountId());
            
            TempData["SuccessMessage"] = $"{IngredientName} quantity updated successfully!";
            return RedirectToPage("/Fridge/Index");
        }
        catch (BusinessException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToPage("/Fridge/Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating fridge item quantity {FridgeItemId} for account {AccountId}", 
                FridgeItemId, GetCurrentAccountId());
            TempData["ErrorMessage"] = "An error occurred while updating the quantity. Please try again.";
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
