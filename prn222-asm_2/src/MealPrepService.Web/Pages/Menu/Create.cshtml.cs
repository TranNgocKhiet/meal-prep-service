using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.Exceptions;

namespace MealPrepService.Web.Pages.Menu;

[Authorize(Roles = "Admin,Manager")]
public class CreateModel : PageModel
{
    private readonly IMenuService _menuService;
    private readonly ILogger<CreateModel> _logger;

    public CreateModel(IMenuService menuService, ILogger<CreateModel> logger)
    {
        _menuService = menuService;
        _logger = logger;
    }

    [BindProperty]
    public DateTime MenuDate { get; set; } = DateTime.Today;

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            // Check if menu already exists for this date
            var existingMenu = await _menuService.GetByDateAsync(MenuDate);
            if (existingMenu != null)
            {
                ModelState.AddModelError(nameof(MenuDate), "A menu already exists for this date.");
                return Page();
            }

            var createdMenu = await _menuService.CreateDailyMenuAsync(MenuDate);
            
            _logger.LogInformation("Daily menu created successfully for date {MenuDate}", MenuDate);
            
            TempData["SuccessMessage"] = "Menu created successfully!";
            return RedirectToPage("/Menu/Details", new { id = createdMenu.Id });
        }
        catch (BusinessException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating menu for date {MenuDate}", MenuDate);
            ModelState.AddModelError(string.Empty, "An error occurred while creating the menu. Please try again.");
            return Page();
        }
    }
}
