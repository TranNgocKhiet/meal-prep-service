using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;

namespace MealPrepService.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class GenerateMonthlyReportModel : PageModel
{
    private readonly IRevenueService _revenueService;
    private readonly ILogger<GenerateMonthlyReportModel> _logger;

    public GenerateMonthlyReportModel(
        IRevenueService revenueService,
        ILogger<GenerateMonthlyReportModel> logger)
    {
        _revenueService = revenueService ?? throw new ArgumentNullException(nameof(revenueService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IActionResult> OnPostAsync(int year, int month)
    {
        try
        {
            var report = await _revenueService.GenerateMonthlyReportAsync(year, month);
            
            TempData["SuccessMessage"] = $"Monthly report for {new DateTime(year, month, 1):MMMM yyyy} generated successfully.";
            _logger.LogInformation("Monthly revenue report generated for {Year}-{Month} by admin {AdminId}", 
                year, month, GetCurrentAccountId());
            
            return RedirectToPage("/Admin/Revenue", new { year });
        }
        catch (BusinessException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            _logger.LogWarning(ex, "Business error generating monthly report for {Year}-{Month}", year, month);
            return RedirectToPage("/Admin/Revenue", new { year });
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "An error occurred while generating the monthly report. Please try again.";
            _logger.LogError(ex, "Unexpected error generating monthly report for {Year}-{Month}", year, month);
            return RedirectToPage("/Admin/Revenue", new { year });
        }
    }

    private Guid GetCurrentAccountId()
    {
        var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(accountIdClaim) || !Guid.TryParse(accountIdClaim, out var accountId))
        {
            throw new AuthenticationException("User account ID not found in claims.");
        }
        return accountId;
    }
}
