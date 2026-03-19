using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;

namespace MealPrepService.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class AccountsModel : PageModel
{
    private static readonly HashSet<string> AllowedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Admin",
        "Staff",
        "Manager",
        "DeliveryMan",
        "Customer"
    };

    private readonly IAccountService _accountService;
    private readonly ILogger<AccountsModel> _logger;

    public List<AccountDto> Accounts { get; set; } = new();
    public string? FilterRole { get; set; }

    public int TotalAccounts => Accounts.Count;
    public int TotalAdmins => Accounts.Count(a => a.Role == "Admin");
    public int TotalStaff => Accounts.Count(a => a.Role == "Staff");
    public int TotalManagers => Accounts.Count(a => a.Role == "Manager");
    public int TotalDeliveryMen => Accounts.Count(a => a.Role == "DeliveryMan");
    public int TotalCustomers => Accounts.Count(a => a.Role == "Customer");

    public AccountsModel(
        IAccountService accountService,
        ILogger<AccountsModel> logger)
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

            if (!string.IsNullOrWhiteSpace(role) && AllowedRoles.Contains(role))
            {
                accounts = await _accountService.GetAccountsByRoleAsync(role);
            }
            else
            {
                // Default view: show staff accounts (Manager and DeliveryMan)
                accounts = await _accountService.GetAllStaffAccountsAsync();
                role = null;
            }

            Accounts = accounts.ToList();
            FilterRole = role;

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while loading accounts");
            TempData["ErrorMessage"] = "An error occurred while loading accounts.";
            return Page();
        }
    }
}