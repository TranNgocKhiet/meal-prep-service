using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;


namespace MealPrepService.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class SystemConfigurationModel : PageModel
{
    private readonly ISystemConfigurationService _systemConfigService;
    private readonly ILogger<SystemConfigurationModel> _logger;

    [BindProperty]
    public int MaxMealPlansPerCustomer { get; set; }

    [BindProperty]
    public int MaxFridgeItemsPerCustomer { get; set; }

    [BindProperty]
    public int MaxMealPlanDays { get; set; }

    public SystemConfigurationModel(
        ISystemConfigurationService systemConfigService,
        ILogger<SystemConfigurationModel> logger)
    {
        _systemConfigService = systemConfigService ?? throw new ArgumentNullException(nameof(systemConfigService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            MaxMealPlansPerCustomer = await _systemConfigService.GetMaxMealPlansPerCustomerAsync();
            MaxFridgeItemsPerCustomer = await _systemConfigService.GetMaxFridgeItemsPerCustomerAsync();
            MaxMealPlanDays = await _systemConfigService.GetMaxMealPlanDaysAsync();

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading system configuration");
            TempData["ErrorMessage"] = "An error occurred while loading system configuration.";
            return RedirectToPage("/Admin/Dashboard");
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var adminEmail = User.Identity?.Name ?? "Admin";

            await _systemConfigService.UpdateMaxMealPlansAsync(MaxMealPlansPerCustomer, adminEmail);
            await _systemConfigService.UpdateMaxFridgeItemsAsync(MaxFridgeItemsPerCustomer, adminEmail);
            await _systemConfigService.UpdateMaxMealPlanDaysAsync(MaxMealPlanDays, adminEmail);

            TempData["SuccessMessage"] = "System configuration updated successfully!";
            _logger.LogInformation("System configuration updated by {AdminEmail}: MaxMealPlans={MaxMealPlans}, MaxFridgeItems={MaxFridgeItems}, MaxMealPlanDays={MaxMealPlanDays}",
                adminEmail, MaxMealPlansPerCustomer, MaxFridgeItemsPerCustomer, MaxMealPlanDays);

            return RedirectToPage("/Admin/SystemConfiguration");
        }
        catch (BusinessException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            _logger.LogWarning(ex, "Business error updating system configuration");
            return Page();
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, "An error occurred while updating system configuration. Please try again.");
            _logger.LogError(ex, "Unexpected error updating system configuration");
            return Page();
        }
    }
}
