using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MealPrepService.DataAccessLayer.Data;

namespace MealPrepService.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class DashboardModel : PageModel
{
    private readonly MealPrepDbContext _dbContext;
    private readonly ILogger<DashboardModel> _logger;

    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public DateTime LastUpdated => DateTime.Now;

    public int TopMonth { get; set; }
    public int TopYear { get; set; }
    public List<int> AvailableTopYears { get; set; } = new();

    public List<MonthlyOrderRevenuePoint> MonthlyOrderRevenue { get; set; } = new();
    public List<MonthlyUsagePoint> MonthlyAiMealPlanUsage { get; set; } = new();
    public List<MonthlyUsagePoint> MonthlyAiNutritionUsage { get; set; } = new();
    public List<MonthlyOrderStatusCountPoint> MonthlyOrderStatusCounts { get; set; } = new();

    public MonthChangeOverview RevenueChangeOverview { get; set; } = new();
    public MonthChangeOverview OrdersChangeOverview { get; set; } = new();

    public List<TopCustomerUsage> TopCustomersAiMealPlanUsage { get; set; } = new();
    public List<TopCustomerUsage> TopCustomersAiNutritionUsage { get; set; } = new();
    public List<TopCustomerSpending> TopCustomerOrderSpending { get; set; } = new();
    public List<TopMealOrder> TopMealsOrdered { get; set; } = new();

    public int MealPlanPage { get; set; }
    public int NutritionPage { get; set; }
    public int SpendingPage { get; set; }
    public int MealPage { get; set; }

    public int MealPlanTotalPages { get; set; }
    public int NutritionTotalPages { get; set; }
    public int SpendingTotalPages { get; set; }
    public int MealTotalPages { get; set; }

    private const int PageSize = 10;

    public class MonthlyOrderRevenuePoint
    {
        public string Label { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int Orders { get; set; }
        public DateTime MonthStart { get; set; }
    }

    public class MonthlyUsagePoint
    {
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }
        public DateTime MonthStart { get; set; }
    }

    public class OrderStatusCountPoint
    {
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class MonthlyOrderStatusCountPoint
    {
        public string Label { get; set; } = string.Empty;
        public DateTime MonthStart { get; set; }
        public int FailedCount { get; set; }
        public int CanceledCount { get; set; }
        public int CustomerReceivedCount { get; set; }
        public int CustomerRejectedCount { get; set; }
    }

    public class MonthChangeOverview
    {
        public string CurrentMonthLabel { get; set; } = string.Empty;
        public string PreviousMonthLabel { get; set; } = string.Empty;
        public decimal CurrentValue { get; set; }
        public decimal PreviousValue { get; set; }
        public decimal Difference => CurrentValue - PreviousValue;
        public bool IsIncrease => Difference >= 0;
    }

    public class TopCustomerUsage
    {
        public string CustomerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int UsageCount { get; set; }
    }

    public class TopCustomerSpending
    {
        public string CustomerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public decimal TotalSpent { get; set; }
    }

    public class TopMealOrder
    {
        public string MealName { get; set; } = string.Empty;
        public int TotalQuantity { get; set; }
    }

    public DashboardModel(MealPrepDbContext dbContext, ILogger<DashboardModel> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync(DateTime? fromDate, DateTime? toDate, int? topMonth, int? topYear,
        int mealPlanPage = 1, int nutritionPage = 1, int spendingPage = 1, int mealPage = 1)
    {
        try
        {
            MealPlanPage = Math.Max(1, mealPlanPage);
            NutritionPage = Math.Max(1, nutritionPage);
            SpendingPage = Math.Max(1, spendingPage);
            MealPage = Math.Max(1, mealPage);
            var today = DateTime.Today;
            FromDate = fromDate?.Date ?? new DateTime(today.Year, today.Month, 1).AddMonths(-5);
            ToDate = toDate?.Date ?? today;

            if (FromDate > ToDate)
            {
                (FromDate, ToDate) = (ToDate, FromDate);
            }

            TopMonth = topMonth ?? ToDate.Month;
            TopYear = topYear ?? ToDate.Year;
            AvailableTopYears = Enumerable.Range(today.Year - 5, 11).ToList();

            if (TopMonth < 1 || TopMonth > 12)
            {
                TopMonth = ToDate.Month;
            }

            var rangeStart = FromDate;
            var rangeEndExclusive = ToDate.AddDays(1);

            await LoadOrderRevenueTrendAsync(rangeStart, rangeEndExclusive);
            await LoadOrderStatusCountsAsync(rangeStart, rangeEndExclusive);
            await LoadAiUsageTrendsAsync(rangeStart, rangeEndExclusive);
            await LoadTop100DashboardsAsync(TopYear, TopMonth);

            await LoadMonthChangeOverviewsAsync();

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while loading admin dashboard");
            TempData["ErrorMessage"] = "An error occurred while loading the dashboard.";
            return Page();
        }
    }

    private async Task LoadOrderRevenueTrendAsync(DateTime rangeStart, DateTime rangeEndExclusive)
    {
        var orders = await _dbContext.Orders
            .AsNoTracking()
            .Where(o => o.OrderDate >= rangeStart && o.OrderDate < rangeEndExclusive)
            .Select(o => new { o.OrderDate, o.TotalAmount })
            .ToListAsync();

        var monthSeries = BuildMonthRange(rangeStart, rangeEndExclusive.AddDays(-1));
        var grouped = orders
            .GroupBy(o => new DateTime(o.OrderDate.Year, o.OrderDate.Month, 1))
            .ToDictionary(
                g => g.Key,
                g => new { Revenue = g.Sum(x => x.TotalAmount), Orders = g.Count() });

        MonthlyOrderRevenue = monthSeries
            .Select(month => new MonthlyOrderRevenuePoint
            {
                MonthStart = month,
                Label = month.ToString("MMM yyyy"),
                Revenue = grouped.TryGetValue(month, out var value) ? value.Revenue : 0m,
                Orders = grouped.TryGetValue(month, out value) ? value.Orders : 0
            })
            .ToList();
    }

    private async Task LoadAiUsageTrendsAsync(DateTime rangeStart, DateTime rangeEndExclusive)
    {
        var logs = await _dbContext.AIOperationLogs
            .AsNoTracking()
            .Where(log => log.Timestamp >= rangeStart && log.Timestamp < rangeEndExclusive)
            .Select(log => new { log.OperationType, log.Timestamp })
            .ToListAsync();

        var monthSeries = BuildMonthRange(rangeStart, rangeEndExclusive.AddDays(-1));

        var mealPlanByMonth = logs
            .Where(log => IsMealPlanOperation(log.OperationType))
            .GroupBy(log => new DateTime(log.Timestamp.Year, log.Timestamp.Month, 1))
            .ToDictionary(g => g.Key, g => g.Count());

        var nutritionByMonth = logs
            .Where(log => IsNutritionOperation(log.OperationType))
            .GroupBy(log => new DateTime(log.Timestamp.Year, log.Timestamp.Month, 1))
            .ToDictionary(g => g.Key, g => g.Count());

        MonthlyAiMealPlanUsage = monthSeries
            .Select(month => new MonthlyUsagePoint
            {
                MonthStart = month,
                Label = month.ToString("MMM yyyy"),
                Count = mealPlanByMonth.TryGetValue(month, out var count) ? count : 0
            })
            .ToList();

        MonthlyAiNutritionUsage = monthSeries
            .Select(month => new MonthlyUsagePoint
            {
                MonthStart = month,
                Label = month.ToString("MMM yyyy"),
                Count = nutritionByMonth.TryGetValue(month, out var count) ? count : 0
            })
            .ToList();
    }

    private async Task LoadOrderStatusCountsAsync(DateTime rangeStart, DateTime rangeEndExclusive)
    {
        var orders = await _dbContext.Orders
            .AsNoTracking()
            .Where(o => o.OrderDate >= rangeStart && o.OrderDate < rangeEndExclusive)
            .Select(o => new { o.OrderDate, o.Status })
            .ToListAsync();

        static bool Matches(string? status, params string[] values)
        {
            if (string.IsNullOrWhiteSpace(status)) return false;
            return values.Any(v => status.Equals(v, StringComparison.OrdinalIgnoreCase));
        }

        var monthSeries = BuildMonthRange(rangeStart, rangeEndExclusive.AddDays(-1));

        var grouped = orders
            .GroupBy(o => new DateTime(o.OrderDate.Year, o.OrderDate.Month, 1))
            .ToDictionary(g => g.Key, g => new
            {
                FailedCount = g.Count(x => Matches(x.Status, "failed", "payment_failed")),
                CanceledCount = g.Count(x => Matches(x.Status, "cancelled", "canceled")),
                CustomerReceivedCount = g.Count(x => Matches(x.Status, "customer_received")),
                CustomerRejectedCount = g.Count(x => Matches(x.Status, "customer_reject", "customer_rejected"))
            });

        MonthlyOrderStatusCounts = monthSeries
            .Select(month => new MonthlyOrderStatusCountPoint
            {
                MonthStart = month,
                Label = month.ToString("MMM yyyy"),
                FailedCount = grouped.TryGetValue(month, out var value) ? value.FailedCount : 0,
                CanceledCount = grouped.TryGetValue(month, out value) ? value.CanceledCount : 0,
                CustomerReceivedCount = grouped.TryGetValue(month, out value) ? value.CustomerReceivedCount : 0,
                CustomerRejectedCount = grouped.TryGetValue(month, out value) ? value.CustomerRejectedCount : 0
            })
            .ToList();
    }

    private async Task LoadTop100DashboardsAsync(int year, int month)
    {
        var monthStart = new DateTime(year, month, 1);
        var monthEndExclusive = monthStart.AddMonths(1);

        var accountMap = await _dbContext.Accounts
            .AsNoTracking()
            .Where(a => a.Role == "Customer")
            .Select(a => new { a.Id, a.FullName, a.Email })
            .ToDictionaryAsync(a => a.Id, a => new { a.FullName, a.Email });

        var aiLogs = await _dbContext.AIOperationLogs
            .AsNoTracking()
            .Where(l => l.CustomerId != null)
            .Select(l => new { l.CustomerId, l.OperationType })
            .ToListAsync();

        var mealPlanList = aiLogs
            .Where(l => l.CustomerId.HasValue && IsMealPlanOperation(l.OperationType))
            .GroupBy(l => l.CustomerId!.Value)
            .OrderByDescending(g => g.Count())
            .Take(100)
            .Select(g => new TopCustomerUsage
            {
                CustomerName = accountMap.TryGetValue(g.Key, out var customer) ? customer.FullName : "Unknown",
                Email = accountMap.TryGetValue(g.Key, out customer) ? customer.Email : string.Empty,
                UsageCount = g.Count()
            })
            .ToList();
            
        MealPlanTotalPages = Math.Max(1, (int)Math.Ceiling(mealPlanList.Count / (double)PageSize));
        TopCustomersAiMealPlanUsage = mealPlanList.Skip((MealPlanPage - 1) * PageSize).Take(PageSize).ToList();

        var nutritionList = aiLogs
            .Where(l => l.CustomerId.HasValue && IsNutritionOperation(l.OperationType))
            .GroupBy(l => l.CustomerId!.Value)
            .OrderByDescending(g => g.Count())
            .Take(100)
            .Select(g => new TopCustomerUsage
            {
                CustomerName = accountMap.TryGetValue(g.Key, out var customer) ? customer.FullName : "Unknown",
                Email = accountMap.TryGetValue(g.Key, out customer) ? customer.Email : string.Empty,
                UsageCount = g.Count()
            })
            .ToList();

        NutritionTotalPages = Math.Max(1, (int)Math.Ceiling(nutritionList.Count / (double)PageSize));
        TopCustomersAiNutritionUsage = nutritionList.Skip((NutritionPage - 1) * PageSize).Take(PageSize).ToList();

        var monthlyOrders = await _dbContext.Orders
            .AsNoTracking()
            .Where(o => o.OrderDate >= monthStart && o.OrderDate < monthEndExclusive)
            .Select(o => new { o.AccountId, o.TotalAmount })
            .ToListAsync();

        var spendingList = monthlyOrders
            .GroupBy(o => o.AccountId)
            .OrderByDescending(g => g.Sum(x => x.TotalAmount))
            .Take(100)
            .Select(g => new TopCustomerSpending
            {
                CustomerName = accountMap.TryGetValue(g.Key, out var customer) ? customer.FullName : "Unknown",
                Email = accountMap.TryGetValue(g.Key, out customer) ? customer.Email : string.Empty,
                TotalSpent = g.Sum(x => x.TotalAmount)
            })
            .ToList();

        SpendingTotalPages = Math.Max(1, (int)Math.Ceiling(spendingList.Count / (double)PageSize));
        TopCustomerOrderSpending = spendingList.Skip((SpendingPage - 1) * PageSize).Take(PageSize).ToList();

        var mealsList = await _dbContext.OrderDetails
            .AsNoTracking()
            .Where(od => od.Order.OrderDate >= monthStart && od.Order.OrderDate < monthEndExclusive)
            .GroupBy(od => od.MenuMeal.Recipe.RecipeName)
            .Select(g => new TopMealOrder
            {
                MealName = g.Key,
                TotalQuantity = g.Sum(x => x.Quantity)
            })
            .OrderByDescending(x => x.TotalQuantity)
            .Take(100)
            .ToListAsync();

        MealTotalPages = Math.Max(1, (int)Math.Ceiling(mealsList.Count / (double)PageSize));
        TopMealsOrdered = mealsList.Skip((MealPage - 1) * PageSize).Take(PageSize).ToList();
    }

    private async Task LoadMonthChangeOverviewsAsync()
    {
        var today = DateTime.Today;
        var currentMonthStart = new DateTime(today.Year, today.Month, 1);
        var previousMonthStart = currentMonthStart.AddMonths(-1);
        var nextMonthStart = currentMonthStart.AddMonths(1);

        var currentMonthOrders = await _dbContext.Orders
            .AsNoTracking()
            .Where(o => o.OrderDate >= currentMonthStart && o.OrderDate < nextMonthStart)
            .Select(o => o.TotalAmount)
            .ToListAsync();

        var previousMonthOrders = await _dbContext.Orders
            .AsNoTracking()
            .Where(o => o.OrderDate >= previousMonthStart && o.OrderDate < currentMonthStart)
            .Select(o => o.TotalAmount)
            .ToListAsync();

        RevenueChangeOverview = new MonthChangeOverview
        {
            CurrentMonthLabel = currentMonthStart.ToString("MMM yyyy"),
            PreviousMonthLabel = previousMonthStart.ToString("MMM yyyy"),
            CurrentValue = currentMonthOrders.Sum(),
            PreviousValue = previousMonthOrders.Sum()
        };

        OrdersChangeOverview = new MonthChangeOverview
        {
            CurrentMonthLabel = currentMonthStart.ToString("MMM yyyy"),
            PreviousMonthLabel = previousMonthStart.ToString("MMM yyyy"),
            CurrentValue = currentMonthOrders.Count,
            PreviousValue = previousMonthOrders.Count
        };
    }

    private static List<DateTime> BuildMonthRange(DateTime start, DateTime end)
    {
        var result = new List<DateTime>();
        var month = new DateTime(start.Year, start.Month, 1);
        var last = new DateTime(end.Year, end.Month, 1);

        while (month <= last)
        {
            result.Add(month);
            month = month.AddMonths(1);
        }

        return result;
    }

    private static bool IsMealPlanOperation(string? operationType)
    {
        if (string.IsNullOrWhiteSpace(operationType))
        {
            return false;
        }

        var value = operationType.Trim().ToLowerInvariant();
        return value.Contains("mealplan")
               || value.Contains("meal plan")
               || value.Contains("recommend");
    }

    private static bool IsNutritionOperation(string? operationType)
    {
        if (string.IsNullOrWhiteSpace(operationType))
        {
            return false;
        }

        var value = operationType.Trim().ToLowerInvariant();
        return value.Contains("nutrition")
               || value.Contains("nutri")
               || value.Contains("analy");
    }
}
