using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;

namespace MealPrepService.Web.Pages.Menu;

[Authorize(Roles = "Admin,Manager")]
public class RemoveMealModel : PageModel
{
    private readonly IMenuService _menuService;
    private readonly ILogger<RemoveMealModel> _logger;

    public RemoveMealModel(IMenuService menuService, ILogger<RemoveMealModel> logger)
    {
        _menuService = menuService;
        _logger = logger;
    }

    public async Task<IActionResult> OnPostAsync(Guid menuMealId, Guid menuId)
    {
        try
        {
            // Find the menu to check its status
            DailyMenuDto? parentMenu = null;
            
            // Search through recent dates to find the menu
            for (var date = DateTime.Today.AddDays(-30); date <= DateTime.Today.AddDays(30); date = date.AddDays(1))
            {
                var menu = await _menuService.GetByDateAsync(date);
                if (menu?.Id == menuId)
                {
                    parentMenu = menu;
                    break;
                }
            }
            
            if (parentMenu == null)
            {
                TempData["ErrorMessage"] = "Menu not found.";
                return RedirectToPage("/Menu/Index");
            }

            // Check if menu is in draft or inactive status (can be edited)
            if (parentMenu.Status.Equals("active", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Cannot remove meals from an active menu. Please deactivate it first.";
                return RedirectToPage("/Menu/Details", new { id = menuId });
            }

            await _menuService.RemoveMealFromMenuAsync(menuMealId);
            
            _logger.LogInformation("Meal {MenuMealId} removed from menu {MenuId}", menuMealId, menuId);
            
            TempData["SuccessMessage"] = "Meal removed from menu successfully!";
            return RedirectToPage("/Menu/Details", new { id = menuId });
        }
        catch (BusinessException ex)
        {
            _logger.LogError(ex, "Business error occurred while removing meal {MenuMealId} from menu {MenuId}", menuMealId, menuId);
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToPage("/Menu/Details", new { id = menuId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while removing meal {MenuMealId} from menu {MenuId}", menuMealId, menuId);
            TempData["ErrorMessage"] = "An error occurred while removing the meal. Please try again.";
            return RedirectToPage("/Menu/Details", new { id = menuId });
        }
    }
}
