using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;


namespace MealPrepService.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class DashboardModel : PageModel
{
    private readonly IRevenueService _revenueService;
    private readonly IAccountService _accountService;
    private readonly ISystemConfigurationService _systemConfigService;
    private readonly ILogger<DashboardModel> _logger;

    public int TotalCustomers { get; set; }
    public int ActiveSubscriptions { get; set; }
    public int PendingOrders { get; set; }
    public decimal CurrentMonthRevenue { get; set; }

    // Helper properties for UI
    public DateTime LastUpdated => DateTime.Now;
    public List<DashboardCard> DashboardCards => new List<DashboardCard>
    {
        new DashboardCard { Title = "Total Customers", Value = TotalCustomers.ToString(), Icon = "bi-people", CssClass = "primary" },
        new DashboardCard { Title = "Active Subscriptions", Value = ActiveSubscriptions.ToString(), Icon = "bi-star", CssClass = "success" },
        new DashboardCard { Title = "Pending Orders", Value = PendingOrders.ToString(), Icon = "bi-cart", CssClass = "warning" },
        new DashboardCard { Title = "Current Month Revenue", Value = CurrentMonthRevenue.ToString("C"), Icon = "bi-cash", CssClass = "info" }
    };

    public class DashboardCard
    {
        public string Title { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string CssClass { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Trend { get; set; } = string.Empty;
        public string TrendCssClass { get; set; } = string.Empty;
    }

    public DashboardModel(
        IRevenueService revenueService, 
        IAccountService accountService, 
        ISystemConfigurationService systemConfigService,
        ILogger<DashboardModel> logger)
    {
        _revenueService = revenueService ?? throw new ArgumentNullException(nameof(revenueService));
        _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
        _systemConfigService = systemConfigService ?? throw new ArgumentNullException(nameof(systemConfigService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            var dashboardStats = await _revenueService.GetDashboardStatsAsync();
            
            TotalCustomers = dashboardStats.TotalCustomers;
            ActiveSubscriptions = dashboardStats.ActiveSubscriptions;
            PendingOrders = dashboardStats.PendingOrders;
            CurrentMonthRevenue = dashboardStats.CurrentMonthRevenue;

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while loading admin dashboard");
            TempData["ErrorMessage"] = "An error occurred while loading the dashboard.";
            return Page();
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
