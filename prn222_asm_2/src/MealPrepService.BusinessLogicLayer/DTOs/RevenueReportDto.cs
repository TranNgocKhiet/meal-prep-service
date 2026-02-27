namespace MealPrepService.BusinessLogicLayer.DTOs
{
    /// <summary>
    /// DTO for revenue report data
    /// </summary>
    public class RevenueReportDto
    {
        public Guid Id { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal TotalSubscriptionRevenue { get; set; }
        public decimal TotalOrderRevenue { get; set; }
        public int TotalOrdersCount { get; set; }
        public decimal TotalRevenue => TotalSubscriptionRevenue + TotalOrderRevenue;

        // Helper properties for UI
        public string MonthName => new DateTime(Year, Month, 1).ToString("MMMM");
        public bool HasOrderRevenue => TotalOrderRevenue > 0;
        public bool HasSubscriptionRevenue => TotalSubscriptionRevenue > 0;
        public bool HasOrders => TotalOrdersCount > 0;
        public decimal AverageOrderValue => TotalOrdersCount > 0 ? TotalOrderRevenue / TotalOrdersCount : 0;
    }
}