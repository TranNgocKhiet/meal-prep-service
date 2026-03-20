namespace MealPrepService.BusinessLogicLayer.DTOs
{
    /// <summary>
    /// DTO for admin dashboard statistics
    /// </summary>
    public class DashboardStatsDto
    {
        public int TotalCustomers { get; set; }
        public int ActiveSubscriptions { get; set; }
        public int PendingOrders { get; set; }
        public decimal CurrentMonthRevenue { get; set; }
    }

    public class AdminDashboardDataDto
    {
        public List<MonthlyOrderRevenuePointDto> MonthlyOrderRevenue { get; set; } = new();
        public List<MonthlyUsagePointDto> AiMealPlanUsage { get; set; } = new();
        public List<MonthlyUsagePointDto> AiNutritionUsage { get; set; } = new();

        public decimal CurrentMonthRevenue { get; set; }
        public decimal PreviousMonthRevenue { get; set; }
        public int CurrentMonthOrders { get; set; }
        public int PreviousMonthOrders { get; set; }

        public List<TopCustomerUsageDto> TopCustomersAiMealPlanUsage { get; set; } = new();
        public List<TopCustomerUsageDto> TopCustomersAiNutritionUsage { get; set; } = new();
        public List<TopCustomerSpendingDto> TopCustomerOrderSpending { get; set; } = new();
        public List<TopMealOrderDto> TopMealsOrdered { get; set; } = new();
    }

    public class MonthlyOrderRevenuePointDto
    {
        public string Label { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int Orders { get; set; }
    }

    public class MonthlyUsagePointDto
    {
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class TopCustomerUsageDto
    {
        public Guid CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int UsageCount { get; set; }
    }

    public class TopCustomerSpendingDto
    {
        public Guid CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public decimal TotalSpent { get; set; }
    }

    public class TopMealOrderDto
    {
        public string MealName { get; set; } = string.Empty;
        public int TotalQuantity { get; set; }
    }
}