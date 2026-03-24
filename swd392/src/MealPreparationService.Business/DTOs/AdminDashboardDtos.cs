namespace MealPreparationService.Business.DTOs;

public class AdminDashboardResponseDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public DateTime LastUpdated { get; set; }

    public int TopMonth { get; set; }
    public int TopYear { get; set; }
    public List<int> AvailableTopYears { get; set; } = new();

    public List<MonthlyOrderRevenuePointDto> MonthlyOrderRevenue { get; set; } = new();
    public List<MonthlyUsagePointDto> MonthlyAiMealPlanUsage { get; set; } = new();
    public List<MonthlyUsagePointDto> MonthlyAiNutritionUsage { get; set; } = new();
    public List<MonthlyOrderStatusCountPointDto> MonthlyOrderStatusCounts { get; set; } = new();

    public MonthChangeOverviewDto RevenueChangeOverview { get; set; } = new();
    public MonthChangeOverviewDto OrdersChangeOverview { get; set; } = new();

    public List<TopCustomerUsageDto> TopCustomersAiMealPlanUsage { get; set; } = new();
    public List<TopCustomerUsageDto> TopCustomersAiNutritionUsage { get; set; } = new();
    public List<TopCustomerSpendingDto> TopCustomerOrderSpending { get; set; } = new();
    public List<TopMealOrderDto> TopMealsOrdered { get; set; } = new();
    public List<TopMealSharePointDto> TopMealsByQuantityInRange { get; set; } = new();
    public List<TopMealSharePointDto> TopMealsByRevenueInRange { get; set; } = new();

    public int MealPlanPage { get; set; }
    public int NutritionPage { get; set; }
    public int SpendingPage { get; set; }
    public int MealPage { get; set; }

    public int MealPlanTotalPages { get; set; }
    public int NutritionTotalPages { get; set; }
    public int SpendingTotalPages { get; set; }
    public int MealTotalPages { get; set; }
}

public class MonthlyOrderRevenuePointDto
{
    public string Label { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int Orders { get; set; }
    public DateTime MonthStart { get; set; }
}

public class MonthlyUsagePointDto
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
    public DateTime MonthStart { get; set; }
}

public class MonthlyOrderStatusCountPointDto
{
    public string Label { get; set; } = string.Empty;
    public DateTime MonthStart { get; set; }
    public int FailedCount { get; set; }
    public int CanceledCount { get; set; }
    public int CustomerReceivedCount { get; set; }
    public int CustomerRejectedCount { get; set; }
}

public class MonthChangeOverviewDto
{
    public string CurrentMonthLabel { get; set; } = string.Empty;
    public string PreviousMonthLabel { get; set; } = string.Empty;
    public decimal CurrentValue { get; set; }
    public decimal PreviousValue { get; set; }
    public decimal Difference { get; set; }
    public bool IsIncrease { get; set; }
}

public class TopCustomerUsageDto
{
    public string CustomerName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int UsageCount { get; set; }
}

public class TopCustomerSpendingDto
{
    public string CustomerName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public decimal TotalSpent { get; set; }
}

public class TopMealOrderDto
{
    public string MealName { get; set; } = string.Empty;
    public int TotalQuantity { get; set; }
}

public class TopMealSharePointDto
{
    public string MealName { get; set; } = string.Empty;
    public int TotalQuantity { get; set; }
    public decimal TotalRevenue { get; set; }
}
