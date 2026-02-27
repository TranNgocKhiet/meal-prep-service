using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;


namespace MealPrepService.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class DeleteStaffAccountModel : PageModel
{
    private readonly IAccountService _accountService;
    private readonly ILogger<DeleteStaffAccountModel> _logger;

    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    // Helper property for UI
    public string RoleBadgeClass => Role switch
    {
        "Manager" => "badge bg-info",
        "DeliveryMan" => "badge bg-success",
        _ => "badge bg-secondary"
    };

    public DeleteStaffAccountModel(
        IAccountService accountService,
        ILogger<DeleteStaffAccountModel> logger)
    {
        _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        try
        {
            var account = await _accountService.GetByIdAsync(id);
            
            if (account.Role != "Manager" && account.Role != "DeliveryMan")
            {
                TempData["ErrorMessage"] = "Only Manager and DeliveryMan accounts can be deleted.";
                return RedirectToPage("/Admin/StaffAccounts");
            }

            Id = account.Id;
            Email = account.Email;
            FullName = account.FullName;
            Role = account.Role;

            return Page();
        }
        catch (BusinessException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToPage("/Admin/StaffAccounts");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while loading staff account for deletion");
            TempData["ErrorMessage"] = "An error occurred while loading the account.";
            return RedirectToPage("/Admin/StaffAccounts");
        }
    }
}
