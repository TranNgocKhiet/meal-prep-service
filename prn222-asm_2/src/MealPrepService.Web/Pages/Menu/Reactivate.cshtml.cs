using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.Exceptions;
using MealPrepService.Web.Hubs;

namespace MealPrepService.Web.Pages.Menu;

[Authorize(Roles = "Admin,Manager")]
public class ReactivateModel : PageModel
{
    private readonly IMenuService _menuService;
    private readonly IHubContext<MenuHub> _menuHubContext;
    private readonly ILogger<ReactivateModel> _logger;

    public ReactivateModel(IMenuService menuService, IHubContext<MenuHub> menuHubContext, ILogger<ReactivateModel> logger)
    {
        _menuService = menuService;
        _menuHubContext = menuHubContext;
        _logger = logger;
    }

    public async Task<IActionResult> OnPostAsync(Guid menuId)
    {
        try
        {
            await _menuService.ReactivateMenuAsync(menuId);
            
            // Send SignalR notification to all clients
            // Note: We send menuId and let clients handle the refresh based on their current view
            await _menuHubContext.Clients.All.SendAsync("ReceiveMenuStatusChange", 
                menuId.ToString(), 
                DateTime.UtcNow.ToString("yyyy-MM-dd"), 
                true);
            
            _logger.LogInformation("Menu {MenuId} reactivated successfully", menuId);
            
            TempData["SuccessMessage"] = "Menu reactivated successfully! It will now appear in public menus.";
            return RedirectToPage("/Menu/Details", new { id = menuId });
        }
        catch (BusinessException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToPage("/Menu/Details", new { id = menuId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while reactivating menu {MenuId}", menuId);
            TempData["ErrorMessage"] = "An error occurred while reactivating the menu. Please try again.";
            return RedirectToPage("/Menu/Details", new { id = menuId });
        }
    }
}
