using MealPreparationService.API.Models;
using MealPreparationService.Business.DTOs;
using MealPreparationService.DataAccess.UnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MealPreparationService.API.Controllers;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Roles = "Admin")]
public class AdminDashboardController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AdminDashboardController> _logger;
    private const int PageSize = 10;

    public AdminDashboardController(IUnitOfWork unitOfWork, ILogger<AdminDashboardController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<AdminDashboardResponseDto>>> GetDashboard(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int? topMonth,
        [FromQuery] int? topYear,
        [FromQuery] int topMealsCount = 5,
        [FromQuery] int? topMealsQuantityCount = null,
        [FromQuery] int? topMealsRevenueCount = null,
        [FromQuery] int mealPlanPage = 1,
        [FromQuery] int nutritionPage = 1,
        [FromQuery] int spendingPage = 1,
        [FromQuery] int mealPage = 1)
    {
        try
        {
            var today = DateTime.Today;
            var safeFromDate = fromDate?.Date ?? new DateTime(today.Year, today.Month, 1).AddMonths(-5);
            var safeToDate = toDate?.Date ?? today;

            if (safeFromDate > safeToDate)
            {
                (safeFromDate, safeToDate) = (safeToDate, safeFromDate);
            }

            var safeTopMonth = topMonth ?? safeToDate.Month;
            if (safeTopMonth is < 1 or > 12)
            {
                safeTopMonth = safeToDate.Month;
            }

            var safeTopYear = topYear ?? safeToDate.Year;
            var safeTopMealsCount = Math.Clamp(topMealsCount, 2, 20);
            var safeTopMealsQuantityCount = Math.Clamp(topMealsQuantityCount ?? safeTopMealsCount, 2, 20);
            var safeTopMealsRevenueCount = Math.Clamp(topMealsRevenueCount ?? safeTopMealsCount, 2, 20);

            var result = new AdminDashboardResponseDto
            {
                FromDate = safeFromDate,
                ToDate = safeToDate,
                LastUpdated = DateTime.Now,
                TopMonth = safeTopMonth,
                TopYear = safeTopYear,
                AvailableTopYears = Enumerable.Range(today.Year - 5, 11).ToList(),
                MealPlanPage = Math.Max(1, mealPlanPage),
                NutritionPage = Math.Max(1, nutritionPage),
                SpendingPage = Math.Max(1, spendingPage),
                MealPage = Math.Max(1, mealPage)
            };

            var rangeStart = safeFromDate;
            var rangeEndExclusive = safeToDate.AddDays(1);

            await LoadOrderRevenueTrendAsync(result, rangeStart, rangeEndExclusive);
            await LoadOrderStatusCountsAsync(result, rangeStart, rangeEndExclusive);
            await LoadAiUsageTrendsAsync(result, rangeStart, rangeEndExclusive);
            await LoadOverviewTopMealsAsync(
                result,
                rangeStart,
                rangeEndExclusive,
                safeTopMealsQuantityCount,
                safeTopMealsRevenueCount);
            await LoadMonthChangeOverviewsAsync(result);
            await LoadTopDashboardsAsync(result, safeTopYear, safeTopMonth);

            return Ok(ApiResponse<AdminDashboardResponseDto>.SuccessResponse(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while loading admin dashboard");
            return StatusCode(500, ApiResponse<AdminDashboardResponseDto>.ErrorResponse(
                "An error occurred while loading the dashboard."));
        }
    }

    private async Task LoadOrderRevenueTrendAsync(AdminDashboardResponseDto result, DateTime rangeStart, DateTime rangeEndExclusive)
    {
        var useDailyGranularity = ShouldUseDailyGranularity(rangeStart, rangeEndExclusive);
        var rangeEnd = rangeEndExclusive.AddDays(-1);

        var orders = await _unitOfWork.Orders.GetAllQueryable()
            .AsNoTracking()
            .Where(o => o.Date >= rangeStart && o.Date < rangeEndExclusive)
            .Select(o => new { o.Date, o.Amount })
            .ToListAsync();

        var periodSeries = BuildPeriodRange(rangeStart, rangeEnd, useDailyGranularity);
        var grouped = orders
            .GroupBy(o => TruncateToPeriod(o.Date, useDailyGranularity))
            .ToDictionary(
                g => g.Key,
                g => new { Revenue = g.Sum(x => x.Amount), Orders = g.Count() });

        result.MonthlyOrderRevenue = periodSeries
            .Select(period => new MonthlyOrderRevenuePointDto
            {
                MonthStart = period,
                Label = BuildPeriodLabel(period, useDailyGranularity),
                Revenue = grouped.TryGetValue(period, out var value) ? value.Revenue : 0m,
                Orders = grouped.TryGetValue(period, out value) ? value.Orders : 0
            })
            .ToList();
    }

    private async Task LoadAiUsageTrendsAsync(AdminDashboardResponseDto result, DateTime rangeStart, DateTime rangeEndExclusive)
    {
        var useDailyGranularity = ShouldUseDailyGranularity(rangeStart, rangeEndExclusive);
        var rangeEnd = rangeEndExclusive.AddDays(-1);
        var periodSeries = BuildPeriodRange(rangeStart, rangeEnd, useDailyGranularity);

        var aiMealPlans = await _unitOfWork.MealPlans.GetAllQueryable()
            .AsNoTracking()
            .Where(mp => mp.IsAiGenerated && mp.CreatedAt >= rangeStart && mp.CreatedAt < rangeEndExclusive)
            .Select(mp => mp.CreatedAt)
            .ToListAsync();

        var aiCreditTransactions = await _unitOfWork.AICreditTransactions.GetAllQueryable()
            .AsNoTracking()
            .Where(tx => tx.CreatedAt >= rangeStart && tx.CreatedAt < rangeEndExclusive)
            .Select(tx => tx.CreatedAt)
            .ToListAsync();

        var mealPlanByMonth = aiMealPlans
            .GroupBy(x => TruncateToPeriod(x, useDailyGranularity))
            .ToDictionary(g => g.Key, g => g.Count());

        var nutritionProxyByMonth = aiCreditTransactions
            .GroupBy(x => TruncateToPeriod(x, useDailyGranularity))
            .ToDictionary(g => g.Key, g => g.Count());

        result.MonthlyAiMealPlanUsage = periodSeries
            .Select(period => new MonthlyUsagePointDto
            {
                MonthStart = period,
                Label = BuildPeriodLabel(period, useDailyGranularity),
                Count = mealPlanByMonth.TryGetValue(period, out var count) ? count : 0
            })
            .ToList();

        result.MonthlyAiNutritionUsage = periodSeries
            .Select(period => new MonthlyUsagePointDto
            {
                MonthStart = period,
                Label = BuildPeriodLabel(period, useDailyGranularity),
                Count = nutritionProxyByMonth.TryGetValue(period, out var count) ? count : 0
            })
            .ToList();
    }

    private async Task LoadOrderStatusCountsAsync(AdminDashboardResponseDto result, DateTime rangeStart, DateTime rangeEndExclusive)
    {
        var useDailyGranularity = ShouldUseDailyGranularity(rangeStart, rangeEndExclusive);
        var rangeEnd = rangeEndExclusive.AddDays(-1);

        var orders = await _unitOfWork.Orders.GetAllQueryable()
            .AsNoTracking()
            .Include(o => o.Status)
            .Where(o => o.Date >= rangeStart && o.Date < rangeEndExclusive)
            .Select(o => new { o.Date, StatusName = o.Status.Name })
            .ToListAsync();

        static bool Matches(string? status, params string[] values)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return false;
            }

            return values.Any(v => status.Contains(v, StringComparison.OrdinalIgnoreCase));
        }

        var periodSeries = BuildPeriodRange(rangeStart, rangeEnd, useDailyGranularity);

        var grouped = orders
            .GroupBy(o => TruncateToPeriod(o.Date, useDailyGranularity))
            .ToDictionary(g => g.Key, g => new
            {
                FailedCount = g.Count(x => Matches(x.StatusName, "failed", "payment_failed")),
                CanceledCount = g.Count(x => Matches(x.StatusName, "cancelled", "canceled")),
                CustomerReceivedCount = g.Count(x => Matches(x.StatusName, "customer_received", "received")),
                CustomerRejectedCount = g.Count(x => Matches(x.StatusName, "customer_reject", "customer_rejected", "rejected"))
            });

        result.MonthlyOrderStatusCounts = periodSeries
            .Select(period => new MonthlyOrderStatusCountPointDto
            {
                MonthStart = period,
                Label = BuildPeriodLabel(period, useDailyGranularity),
                FailedCount = grouped.TryGetValue(period, out var value) ? value.FailedCount : 0,
                CanceledCount = grouped.TryGetValue(period, out value) ? value.CanceledCount : 0,
                CustomerReceivedCount = grouped.TryGetValue(period, out value) ? value.CustomerReceivedCount : 0,
                CustomerRejectedCount = grouped.TryGetValue(period, out value) ? value.CustomerRejectedCount : 0
            })
            .ToList();
    }

    private async Task LoadTopDashboardsAsync(AdminDashboardResponseDto result, int year, int month)
    {
        var monthStart = new DateTime(year, month, 1);
        var monthEndExclusive = monthStart.AddMonths(1);

        var accountMap = await _unitOfWork.Accounts.GetAllQueryable()
            .AsNoTracking()
            .Include(a => a.Role)
            .Where(a => a.Role.Name == "Customer")
            .Select(a => new { a.Id, a.FullName, a.Email })
            .ToDictionaryAsync(a => a.Id, a => new { a.FullName, a.Email });

        var aiMealPlanList = await _unitOfWork.MealPlans.GetAllQueryable()
            .AsNoTracking()
            .Where(mp => mp.IsAiGenerated)
            .GroupBy(mp => mp.AccountId)
            .OrderByDescending(g => g.Count())
            .Take(100)
            .Select(g => new { CustomerId = g.Key, UsageCount = g.Count() })
            .ToListAsync();

        var mealPlanRows = aiMealPlanList
            .Select(g => new TopCustomerUsageDto
            {
                CustomerName = accountMap.TryGetValue(g.CustomerId, out var customer) ? customer.FullName : "Unknown",
                Email = accountMap.TryGetValue(g.CustomerId, out customer) ? customer.Email : string.Empty,
                UsageCount = g.UsageCount
            })
            .ToList();

        result.MealPlanTotalPages = Math.Max(1, (int)Math.Ceiling(mealPlanRows.Count / (double)PageSize));
        result.TopCustomersAiMealPlanUsage = mealPlanRows
            .Skip((result.MealPlanPage - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        var aiNutritionList = await _unitOfWork.AICreditTransactions.GetAllQueryable()
            .AsNoTracking()
            .GroupBy(tx => tx.AccountId)
            .OrderByDescending(g => g.Count())
            .Take(100)
            .Select(g => new { CustomerId = g.Key, UsageCount = g.Count() })
            .ToListAsync();

        var nutritionRows = aiNutritionList
            .Select(g => new TopCustomerUsageDto
            {
                CustomerName = accountMap.TryGetValue(g.CustomerId, out var customer) ? customer.FullName : "Unknown",
                Email = accountMap.TryGetValue(g.CustomerId, out customer) ? customer.Email : string.Empty,
                UsageCount = g.UsageCount
            })
            .ToList();

        result.NutritionTotalPages = Math.Max(1, (int)Math.Ceiling(nutritionRows.Count / (double)PageSize));
        result.TopCustomersAiNutritionUsage = nutritionRows
            .Skip((result.NutritionPage - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        var monthlyOrders = await _unitOfWork.Orders.GetAllQueryable()
            .AsNoTracking()
            .Where(o => o.Date >= monthStart && o.Date < monthEndExclusive)
            .Select(o => new { o.CustomerId, o.Amount })
            .ToListAsync();

        var spendingRows = monthlyOrders
            .GroupBy(o => o.CustomerId)
            .OrderByDescending(g => g.Sum(x => x.Amount))
            .Take(100)
            .Select(g => new TopCustomerSpendingDto
            {
                CustomerName = accountMap.TryGetValue(g.Key, out var customer) ? customer.FullName : "Unknown",
                Email = accountMap.TryGetValue(g.Key, out customer) ? customer.Email : string.Empty,
                TotalSpent = g.Sum(x => x.Amount)
            })
            .ToList();

        result.SpendingTotalPages = Math.Max(1, (int)Math.Ceiling(spendingRows.Count / (double)PageSize));
        result.TopCustomerOrderSpending = spendingRows
            .Skip((result.SpendingPage - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        var mealRows = await _unitOfWork.OrderDetails.GetAllQueryable()
            .AsNoTracking()
            .Include(od => od.Order)
            .Include(od => od.MenuMeal)
                .ThenInclude(mm => mm.MenuMealRecipes)
                .ThenInclude(mmr => mmr.Recipe)
            .Where(od => od.Order.Date >= monthStart && od.Order.Date < monthEndExclusive)
            .Select(od => new
            {
                od.Quantity,
                Recipes = od.MenuMeal.MenuMealRecipes.Select(mmr => mmr.Recipe.RecipeName).ToList()
            })
            .ToListAsync();

        var topMeals = mealRows
            .SelectMany(m =>
            {
                var names = m.Recipes.Any() ? m.Recipes : new List<string> { "Unknown meal" };
                return names.Select(name => new { MealName = name, Quantity = m.Quantity });
            })
            .GroupBy(x => x.MealName)
            .Select(g => new TopMealOrderDto
            {
                MealName = g.Key,
                TotalQuantity = g.Sum(x => x.Quantity)
            })
            .OrderByDescending(x => x.TotalQuantity)
            .Take(100)
            .ToList();

        result.MealTotalPages = Math.Max(1, (int)Math.Ceiling(topMeals.Count / (double)PageSize));
        result.TopMealsOrdered = topMeals
            .Skip((result.MealPage - 1) * PageSize)
            .Take(PageSize)
            .ToList();
    }

    private async Task LoadOverviewTopMealsAsync(
        AdminDashboardResponseDto result,
        DateTime rangeStart,
        DateTime rangeEndExclusive,
        int topMealsQuantityCount,
        int topMealsRevenueCount)
    {
        var mealRows = await _unitOfWork.OrderDetails.GetAllQueryable()
            .AsNoTracking()
            .Include(od => od.Order)
            .Include(od => od.MenuMeal)
                .ThenInclude(mm => mm.MenuMealRecipes)
                .ThenInclude(mmr => mmr.Recipe)
            .Where(od => od.Order.Date >= rangeStart && od.Order.Date < rangeEndExclusive)
            .Select(od => new
            {
                od.Quantity,
                od.UnitPrice,
                Recipes = od.MenuMeal.MenuMealRecipes.Select(mmr => mmr.Recipe.RecipeName).ToList()
            })
            .ToListAsync();

        var mealAggregates = mealRows
            .Select(od =>
            {
                var recipeNames = od.Recipes
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Select(name => name.Trim())
                    .Distinct()
                    .ToList();

                var mealName = recipeNames.Count > 0
                    ? string.Join(" + ", recipeNames)
                    : "Unknown meal";

                return new
                {
                    MealName = mealName,
                    Quantity = od.Quantity,
                    Revenue = od.UnitPrice * od.Quantity
                };
            })
            .GroupBy(x => x.MealName)
            .Select(g => new TopMealSharePointDto
            {
                MealName = g.Key,
                TotalQuantity = g.Sum(x => x.Quantity),
                TotalRevenue = g.Sum(x => x.Revenue)
            })
            .ToList();

        result.TopMealsByQuantityInRange = mealAggregates
            .OrderByDescending(x => x.TotalQuantity)
            .ThenBy(x => x.MealName)
            .Take(topMealsQuantityCount)
            .ToList();

        result.TopMealsByRevenueInRange = mealAggregates
            .OrderByDescending(x => x.TotalRevenue)
            .ThenBy(x => x.MealName)
            .Take(topMealsRevenueCount)
            .ToList();
    }

    private async Task LoadMonthChangeOverviewsAsync(AdminDashboardResponseDto result)
    {
        var today = DateTime.Today;
        var currentMonthStart = new DateTime(today.Year, today.Month, 1);
        var previousMonthStart = currentMonthStart.AddMonths(-1);
        var nextMonthStart = currentMonthStart.AddMonths(1);

        var currentMonthOrders = await _unitOfWork.Orders.GetAllQueryable()
            .AsNoTracking()
            .Where(o => o.Date >= currentMonthStart && o.Date < nextMonthStart)
            .Select(o => o.Amount)
            .ToListAsync();

        var previousMonthOrders = await _unitOfWork.Orders.GetAllQueryable()
            .AsNoTracking()
            .Where(o => o.Date >= previousMonthStart && o.Date < currentMonthStart)
            .Select(o => o.Amount)
            .ToListAsync();

        var currentRevenue = currentMonthOrders.Sum();
        var previousRevenue = previousMonthOrders.Sum();

        result.RevenueChangeOverview = new MonthChangeOverviewDto
        {
            CurrentMonthLabel = currentMonthStart.ToString("MMM yyyy"),
            PreviousMonthLabel = previousMonthStart.ToString("MMM yyyy"),
            CurrentValue = currentRevenue,
            PreviousValue = previousRevenue,
            Difference = currentRevenue - previousRevenue,
            IsIncrease = currentRevenue - previousRevenue >= 0
        };

        result.OrdersChangeOverview = new MonthChangeOverviewDto
        {
            CurrentMonthLabel = currentMonthStart.ToString("MMM yyyy"),
            PreviousMonthLabel = previousMonthStart.ToString("MMM yyyy"),
            CurrentValue = currentMonthOrders.Count,
            PreviousValue = previousMonthOrders.Count,
            Difference = currentMonthOrders.Count - previousMonthOrders.Count,
            IsIncrease = currentMonthOrders.Count - previousMonthOrders.Count >= 0
        };
    }

    private static bool ShouldUseDailyGranularity(DateTime rangeStart, DateTime rangeEndExclusive)
    {
        var rangeEnd = rangeEndExclusive.AddDays(-1);
        return rangeStart.Year == rangeEnd.Year && rangeStart.Month == rangeEnd.Month;
    }

    private static DateTime TruncateToPeriod(DateTime value, bool useDailyGranularity)
    {
        return useDailyGranularity
            ? value.Date
            : new DateTime(value.Year, value.Month, 1);
    }

    private static string BuildPeriodLabel(DateTime value, bool useDailyGranularity)
    {
        return useDailyGranularity
            ? value.ToString("dd MMM")
            : value.ToString("MMM yyyy");
    }

    private static List<DateTime> BuildPeriodRange(DateTime start, DateTime end, bool useDailyGranularity)
    {
        var result = new List<DateTime>();
        var cursor = useDailyGranularity
            ? start.Date
            : new DateTime(start.Year, start.Month, 1);

        var last = useDailyGranularity
            ? end.Date
            : new DateTime(end.Year, end.Month, 1);

        while (cursor <= last)
        {
            result.Add(cursor);
            cursor = useDailyGranularity ? cursor.AddDays(1) : cursor.AddMonths(1);
        }

        return result;
    }
}
