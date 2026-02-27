using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.Exceptions;
using MealPrepService.Web.Hubs;

namespace MealPrepService.Web.Pages.Menu;

[Authorize(Roles = "Admin,Manager")]
public class PublishModel : PageModel
{
    private readonly IMenuService _menuService;
    private readonly IHubContext<MenuHub> _menuHubContext;
    private readonly ILogger<PublishModel> _logger;

    public PublishModel(IMenuService menuService, IHubContext<MenuHub> menuHubContext, ILogger<PublishModel> logger)
    {
        _menuService = menuService;
        _menuHubContext = menuHubContext;
        _logger = logger;
    }

    public async Task<IActionResult> OnPostAsync(Guid menuId)
    {
        try
        {
            await _menuService.PublishMenuAsync(menuId);
            
            _logger.LogInformation("Menu {MenuId} published successfully", menuId);
            
            // Send SignalR notification to all clients about menu publish
            await _menuHubContext.Clients.All.SendAsync("ReceiveMenuStatusChange", 
                menuId.ToString(), 
                DateTime.UtcNow.ToString("yyyy-MM-dd"), 
                true);
            
            _logger.LogInformation("SignalR notification sent for menu publish");
            
            TempData["SuccessMessage"] = "Menu published successfully!";
            return RedirectToPage("/Menu/Details", new { id = menuId });
        }
        catch (BusinessException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToPage("/Menu/Details", new { id = menuId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while publishing menu {MenuId}", menuId);
            TempData["ErrorMessage"] = "An error occurred while publishing the menu. Please try again.";
            return RedirectToPage("/Menu/Details", new { id = menuId });
        }
    }
}
