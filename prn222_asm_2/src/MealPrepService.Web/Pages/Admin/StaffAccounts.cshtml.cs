using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;


namespace MealPrepService.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class StaffAccountsModel : PageModel
{
    private readonly IAccountService _accountService;
    private readonly ILogger<StaffAccountsModel> _logger;

    public List<AccountDto> Accounts { get; set; } = new();
    public string? FilterRole { get; set; }

    // Helper properties for statistics
    public int TotalStaff => Accounts.Count(a => a.Role == "Manager" || a.Role == "DeliveryMan");
    public int TotalManagers => Accounts.Count(a => a.Role == "Manager");
    public int TotalDeliveryMen => Accounts.Count(a => a.Role == "DeliveryMan");

    // New: total customers across the system (always populated)
    public int TotalCustomers { get; set; }

    public StaffAccountsModel(
        IAccountService accountService,
        ILogger<StaffAccountsModel> logger)
    {
        _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IActionResult> OnGetAsync(string? role)
    {
        try
        {
            // Always get total customers count for the summary card
            var customers = await _accountService.GetAccountsByRoleAsync("Customer");
            TotalCustomers = customers?.Count() ?? 0;

            IEnumerable<AccountDto> accounts;

            // Allow filtering for Manager, DeliveryMan and Customer
            if (!string.IsNullOrWhiteSpace(role) && (role == "Manager" || role == "DeliveryMan" || role == "Customer"))
            {
                accounts = await _accountService.GetAccountsByRoleAsync(role);
            }
            else
            {
                // Default view: show staff accounts (Manager and DeliveryMan)
                accounts = await _accountService.GetAllStaffAccountsAsync();
            }

            Accounts = accounts.ToList();
            FilterRole = role;

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while loading staff accounts");
            TempData["ErrorMessage"] = "An error occurred while loading staff accounts.";
            return Page();
        }
    }
}
