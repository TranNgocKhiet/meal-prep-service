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
    public int SelectedYear { get; set; }
    public List<int> AvailableYears { get; set; } = new();
    public decimal YearlyTotalRevenue { get; set; }
    public decimal YearlySubscriptionRevenue { get; set; }
    public decimal YearlyOrderRevenue { get; set; }
    public int YearlyTotalOrders { get; set; }

    // Helper properties
    public bool HasReports => MonthlyReports.Any();
    public List<MonthlyChartData> ChartData => MonthlyReports.Select(r => new MonthlyChartData
    {
        Month = r.MonthName,
        SubscriptionRevenue = r.TotalSubscriptionRevenue,
        OrderRevenue = r.TotalOrderRevenue,
        TotalRevenue = r.TotalRevenue,
        OrderCount = r.TotalOrdersCount
    }).ToList();

    public class MonthlyChartData
    {
        public string Month { get; set; } = string.Empty;
        public decimal SubscriptionRevenue { get; set; }
        public decimal OrderRevenue { get; set; }
        public decimal TotalRevenue { get; set; }
        public int OrderCount { get; set; }
    }

    // Helper property for yearly summary
    public YearlySummaryData YearlySummary => new YearlySummaryData
    {
        Year = SelectedYear,
        TotalRevenue = YearlyTotalRevenue,
        TotalSubscriptionRevenue = YearlySubscriptionRevenue,
        TotalOrderRevenue = YearlyOrderRevenue,
        TotalOrders = YearlyTotalOrders,
        AverageMonthlyRevenue = MonthlyReports.Any() ? MonthlyReports.Average(r => r.TotalSubscriptionRevenue + r.TotalOrderRevenue) : 0,
        BestMonth = MonthlyReports.Any() ? MonthlyReports.OrderByDescending(r => r.TotalSubscriptionRevenue + r.TotalOrderRevenue).First().Month.ToString() : string.Empty,
        BestMonthRevenue = MonthlyReports.Any() ? MonthlyReports.Max(r => r.TotalSubscriptionRevenue + r.TotalOrderRevenue) : 0
    };

    public class YearlySummaryData
    {
        public int Year { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalSubscriptionRevenue { get; set; }
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

    public async Task<IActionResult> OnGetAsync(int? year)
    {
        try
        {
            SelectedYear = year ?? DateTime.Now.Year;
            var monthlyReports = new List<RevenueReportDto>();
            
            for (int month = 1; month <= 12; month++)
            {
                try
                {
                    var report = await _revenueService.GetMonthlyReportAsync(SelectedYear, month);
                    if (report != null)
                    {
                        monthlyReports.Add(report);
                    }
                }
                catch (BusinessException)
                {
                    continue;
                }
            }

            MonthlyReports = monthlyReports.OrderBy(r => r.Month).ToList();
            YearlyTotalRevenue = await _revenueService.GetYearlyRevenueAsync(SelectedYear);
            YearlySubscriptionRevenue = monthlyReports.Sum(r => r.TotalSubscriptionRevenue);
            YearlyOrderRevenue = monthlyReports.Sum(r => r.TotalOrderRevenue);
            YearlyTotalOrders = monthlyReports.Sum(r => r.TotalOrdersCount);
            AvailableYears = GetAvailableYears();

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while loading revenue reports for year {Year}", year);
            TempData["ErrorMessage"] = "An error occurred while loading revenue reports.";
            SelectedYear = year ?? DateTime.Now.Year;
            AvailableYears = GetAvailableYears();
            return Page();
        }
    }

    private List<int> GetAvailableYears()
    {
        var currentYear = DateTime.Now.Year;
        return new List<int> { currentYear - 2, currentYear - 1, currentYear };
    }
}
