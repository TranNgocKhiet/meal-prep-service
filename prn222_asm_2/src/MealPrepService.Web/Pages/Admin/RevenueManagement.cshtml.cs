using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;
using MealPrepService.DataAccessLayer.Repositories;

namespace MealPrepService.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class RevenueManagementModel : PageModel
{
    private readonly IRevenueService _revenueService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RevenueManagementModel> _logger;

    public List<RevenueReportDto> AllReports { get; set; } = new();
    public int TotalReports { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }

    public RevenueManagementModel(
        IRevenueService revenueService,
        IUnitOfWork unitOfWork,
        ILogger<RevenueManagementModel> logger)
    {
        _revenueService = revenueService ?? throw new ArgumentNullException(nameof(revenueService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            // Load ALL revenue reports from database (including duplicates)
            var allReports = await _unitOfWork.RevenueReports.GetAllAsync();
            
            AllReports = allReports
                .OrderByDescending(r => r.Year)
                .ThenByDescending(r => r.Month)
                .Select(r => new RevenueReportDto
                {
                    Id = r.Id,
                    Month = r.Month,
                    Year = r.Year,
                    TotalSubscriptionRevenue = r.TotalSubscriptionRev,
                    TotalOrderRevenue = r.TotalOrderRev,
                    TotalOrdersCount = r.TotalOrdersCount
                })
                .ToList();

            TotalReports = AllReports.Count;
            TotalRevenue = AllReports.Sum(r => r.TotalRevenue);
            TotalOrders = AllReports.Sum(r => r.TotalOrdersCount);

            _logger.LogInformation("Revenue management page loaded. Total reports: {TotalReports}", TotalReports);
            
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading revenue reports for management");
            TempData["ErrorMessage"] = "An error occurred while loading revenue reports.";
            return Page();
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid reportId)
    {
        try
        {
            // Delete the specific report
            await _unitOfWork.RevenueReports.DeleteAsync(reportId);
            await _unitOfWork.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Revenue report deleted successfully.";
            _logger.LogInformation("Revenue report {ReportId} deleted by admin {AdminId}", 
                reportId, GetCurrentAccountId());
            
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting revenue report {ReportId}", reportId);
            TempData["ErrorMessage"] = "An error occurred while deleting the report.";
            return RedirectToPage();
        }
    }

    public async Task<IActionResult> OnPostDeleteDuplicatesAsync(int year, int month)
    {
        try
        {
            // Get all reports for this month/year
            var reportsToDelete = await _unitOfWork.RevenueReports
                .FindAsync(r => r.Year == year && r.Month == month);
            
            var reportsList = reportsToDelete.ToList();
            
            if (reportsList.Count <= 1)
            {
                TempData["InfoMessage"] = "No duplicates found for this month.";
                return RedirectToPage();
            }

            // Keep the latest one, delete the rest
            var keepReport = reportsList.OrderByDescending(r => r.CreatedAt).First();
            var deleteReports = reportsList.Where(r => r.Id != keepReport.Id).ToList();

            foreach (var report in deleteReports)
            {
                await _unitOfWork.RevenueReports.DeleteAsync(report.Id);
            }
            
            await _unitOfWork.SaveChangesAsync();
            
            TempData["SuccessMessage"] = $"Deleted {deleteReports.Count} duplicate report(s) for {new DateTime(year, month, 1):MMMM yyyy}. Kept the latest one.";
            _logger.LogInformation("Deleted {Count} duplicate reports for {Year}-{Month} by admin {AdminId}", 
                deleteReports.Count, year, month, GetCurrentAccountId());
            
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting duplicate reports for {Year}-{Month}", year, month);
            TempData["ErrorMessage"] = "An error occurred while deleting duplicate reports.";
            return RedirectToPage();
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
