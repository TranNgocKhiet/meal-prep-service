using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;


namespace MealPrepService.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class DeleteAccountModel : PageModel
{
    private readonly IAccountService _accountService;
    private readonly ILogger<DeleteAccountModel> _logger;

    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    // Helper property for UI
    public string RoleBadgeClass => Role switch
    {
        "Admin" => "badge bg-danger",
        "Staff" => "badge bg-secondary",
        "Manager" => "badge bg-info",
        "DeliveryMan" => "badge bg-success",
        "Customer" => "badge bg-primary",
        _ => "badge bg-secondary"
    };

    public DeleteAccountModel(
        IAccountService accountService,
        ILogger<DeleteAccountModel> logger)
    {
        _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        try
        {
            var account = await _accountService.GetByIdAsync(id);

            Id = account.Id;
            Email = account.Email;
            FullName = account.FullName;
            Role = account.Role;

            return Page();
        }
        catch (BusinessException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToPage("/Admin/Accounts");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while loading account for deletion");
            TempData["ErrorMessage"] = "An error occurred while loading the account.";
            return RedirectToPage("/Admin/Accounts");
        }
    }
}
