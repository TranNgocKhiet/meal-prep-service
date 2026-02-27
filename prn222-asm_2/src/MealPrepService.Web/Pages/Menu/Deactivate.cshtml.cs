using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.Exceptions;
using MealPrepService.Web.Hubs;

namespace MealPrepService.Web.Pages.Menu;

[Authorize(Roles = "Admin,Manager")]
public class DeactivateModel : PageModel
{
    private readonly IMenuService _menuService;
    private readonly IHubContext<MenuHub> _menuHubContext;
    private readonly ILogger<DeactivateModel> _logger;

    public DeactivateModel(IMenuService menuService, IHubContext<MenuHub> menuHubContext, ILogger<DeactivateModel> logger)
    {
        _menuService = menuService;
        _menuHubContext = menuHubContext;
        _logger = logger;
    }

    public async Task<IActionResult> OnPostAsync(Guid menuId)
    {
        try
        {
            await _menuService.DeactivateMenuAsync(menuId);
            
            _logger.LogInformation("Menu {MenuId} deactivated successfully", menuId);
            
            // Send SignalR notification to all clients
            var notificationDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
            _logger.LogInformation("Sending SignalR notification: MenuId={MenuId}, Date={Date}, IsActive=false", 
                menuId, notificationDate);
            
            await _menuHubContext.Clients.All.SendAsync("ReceiveMenuStatusChange", 
                menuId.ToString(), 
                notificationDate, 
                false);
            
            _logger.LogInformation("SignalR notification sent successfully");
            
            TempData["SuccessMessage"] = "Menu deactivated successfully! It will no longer appear in public menus.";
            return RedirectToPage("/Menu/Details", new { id = menuId });
        }
        catch (BusinessException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToPage("/Menu/Details", new { id = menuId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deactivating menu {MenuId}", menuId);
            TempData["ErrorMessage"] = "An error occurred while deactivating the menu. Please try again.";
            return RedirectToPage("/Menu/Details", new { id = menuId });
        }
    }
}
