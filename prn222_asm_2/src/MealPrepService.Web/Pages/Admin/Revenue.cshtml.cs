using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;


namespace MealPrepService.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class RevenueModel : PageModel
{
    private readonly IRevenueService _revenueService;
    private readonly ILogger<RevenueModel> _logger;

    public List<RevenueReportDto> MonthlyReports { get; set; } = new();
    public List<RevenueReportDto> FilteredReports { get; set; } = new();
    public int SelectedYear { get; set; }
    public int SelectedMonth { get; set; }
    public List<int> AvailableYears { get; set; } = new();
    public decimal YearlyTotalRevenue { get; set; }
    public decimal YearlyOrderRevenue { get; set; }
    public int YearlyTotalOrders { get; set; }
    
    // Filter properties
    public int? FilterStartYear { get; set; }
    public int? FilterStartMonth { get; set; }
    public int? FilterEndYear { get; set; }
    public int? FilterEndMonth { get; set; }
    public bool IsFiltering { get; set; }

    // Helper properties
    public bool HasReports => MonthlyReports.Any();
    public List<MonthlyChartData> ChartData => FilteredReports.Any() ? FilteredReports.Select(r => new MonthlyChartData
    {
        Month = r.MonthName,
        OrderRevenue = r.TotalOrderRevenue,
        TotalRevenue = r.TotalRevenue,
        OrderCount = r.TotalOrdersCount
    }).ToList() : new List<MonthlyChartData>();

    public class MonthlyChartData
    {
        public string Month { get; set; } = string.Empty;
        public decimal OrderRevenue { get; set; }
        public decimal TotalRevenue { get; set; }
        public int OrderCount { get; set; }
    }

    // Helper property for yearly summary
    public YearlySummaryData YearlySummary => new YearlySummaryData
    {
        Year = SelectedYear,
        TotalRevenue = FilteredReports.Sum(r => r.TotalRevenue),
        TotalOrderRevenue = FilteredReports.Sum(r => r.TotalOrderRevenue),
        TotalOrders = FilteredReports.Sum(r => r.TotalOrdersCount),
        AverageMonthlyRevenue = FilteredReports.Any() ? FilteredReports.Average(r => r.TotalOrderRevenue) : 0,
        BestMonth = FilteredReports.Any() ? FilteredReports.OrderByDescending(r => r.TotalOrderRevenue).First().MonthName : string.Empty,
        BestMonthRevenue = FilteredReports.Any() ? FilteredReports.Max(r => r.TotalOrderRevenue) : 0
    };

    public class YearlySummaryData
    {
        public int Year { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalOrderRevenue { get; set; }
        public int TotalOrders { get; set; }
        public decimal AverageMonthlyRevenue { get; set; }
        public string BestMonth { get; set; } = string.Empty;
        public decimal BestMonthRevenue { get; set; }
    }

    public RevenueModel(
        IRevenueService revenueService,
        ILogger<RevenueModel> logger)
    {
        _revenueService = revenueService ?? throw new ArgumentNullException(nameof(revenueService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IActionResult> OnGetAsync(int? year, int? month, int? filterStartYear, int? filterStartMonth, int? filterEndYear, int? filterEndMonth)
    {
        try
        {
            SelectedYear = year ?? DateTime.Now.Year;
            SelectedMonth = month ?? DateTime.Now.Month;
            AvailableYears = GetAvailableYears();

            // Load all reports that have been saved
            MonthlyReports = await LoadAllSavedReports();

            // Apply filters if provided
            if (filterStartYear.HasValue || filterStartMonth.HasValue || filterEndYear.HasValue || filterEndMonth.HasValue)
            {
                IsFiltering = true;
                FilterStartYear = filterStartYear;
                FilterStartMonth = filterStartMonth ?? 1;
                FilterEndYear = filterEndYear;
                FilterEndMonth = filterEndMonth ?? 12;

                FilteredReports = ApplyDateRangeFilter(MonthlyReports, FilterStartYear, FilterStartMonth, FilterEndYear, FilterEndMonth);
            }
            else
            {
                // Show all reports by default
                FilteredReports = MonthlyReports;
            }

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while loading revenue reports");
            TempData["ErrorMessage"] = "An error occurred while loading revenue reports.";
            SelectedYear = year ?? DateTime.Now.Year;
            AvailableYears = GetAvailableYears();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostGenerateAsync(int year, int month)
    {
        try
        {
            var report = await _revenueService.GenerateMonthlyReportAsync(year, month);
            
            TempData["SuccessMessage"] = $"Revenue report for {new DateTime(year, month, 1):MMMM yyyy} generated and saved successfully.";
            _logger.LogInformation("Monthly revenue report generated for {Year}-{Month} by admin {AdminId}", 
                year, month, GetCurrentAccountId());
            
            return RedirectToPage(new { year, month });
        }
        catch (BusinessException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            _logger.LogWarning(ex, "Business error generating monthly report for {Year}-{Month}", year, month);
            return RedirectToPage(new { year, month });
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "An error occurred while generating the report. Please try again.";
            _logger.LogError(ex, "Unexpected error generating monthly report for {Year}-{Month}", year, month);
            return RedirectToPage(new { year, month });
        }
    }

    private async Task<List<RevenueReportDto>> LoadAllSavedReports()
    {
        var allReports = new List<RevenueReportDto>();

        // Try to load reports from all available years
        foreach (var checkYear in AvailableYears)
        {
            for (int month = 1; month <= 12; month++)
            {
                try
                {
                    var report = await _revenueService.GetMonthlyReportAsync(checkYear, month);
                    if (report != null)
                    {
                        allReports.Add(report);
                    }
                }
                catch (BusinessException)
                {
                    // Report doesn't exist, continue
                    continue;
                }
            }
        }

        return allReports.OrderByDescending(r => r.Year).ThenByDescending(r => r.Month).ToList();
    }

    private List<RevenueReportDto> ApplyDateRangeFilter(List<RevenueReportDto> reports, int? startYear, int? startMonth, int? endYear, int? endMonth)
    {
        return reports.Where(r =>
        {
            // Start date check
            if (startYear.HasValue)
            {
                if (r.Year < startYear) return false;
                if (r.Year == startYear && startMonth.HasValue && r.Month < startMonth) return false;
            }

            // End date check
            if (endYear.HasValue)
            {
                if (r.Year > endYear) return false;
                if (r.Year == endYear && endMonth.HasValue && r.Month > endMonth) return false;
            }

            return true;
        }).OrderByDescending(r => r.Year).ThenByDescending(r => r.Month).ToList();
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

    private List<int> GetAvailableYears()
    {
        var currentYear = DateTime.Now.Year;
        return new List<int> { currentYear - 2, currentYear - 1, currentYear };
    }
}
