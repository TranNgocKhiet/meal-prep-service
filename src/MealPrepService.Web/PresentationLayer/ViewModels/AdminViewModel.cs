using System.ComponentModel.DataAnnotations;

namespace MealPrepService.Web.PresentationLayer.ViewModels
{
    public class AdminDashboardViewModel
    {
        [Display(Name = "Total Customers")]
        public int TotalCustomers { get; set; }

        [Display(Name = "Active Subscriptions")]
        public int ActiveSubscriptions { get; set; }

        [Display(Name = "Pending Orders")]
        public int PendingOrders { get; set; }

        [Display(Name = "Current Month Revenue")]
        [DataType(DataType.Currency)]
        public decimal CurrentMonthRevenue { get; set; }

        [Display(Name = "Current Month")]
        public string CurrentMonth { get; set; } = DateTime.Now.ToString("MMMM yyyy");

        [Display(Name = "Last Updated")]
        [DataType(DataType.DateTime)]
        public DateTime LastUpdated { get; set; } = DateTime.Now;

        // Additional computed properties for dashboard display
        public string RevenueGrowthIndicator { get; set; } = "stable";
        public string RevenueGrowthCssClass => RevenueGrowthIndicator switch
        {
            "up" => "text-success",
            "down" => "text-danger",
            _ => "text-muted"
        };

        public bool HasActiveSubscriptions => ActiveSubscriptions > 0;
        public bool HasPendingOrders => PendingOrders > 0;
        public bool HasRevenue => CurrentMonthRevenue > 0;

        // Quick stats for cards
        public List<DashboardCardViewModel> DashboardCards { get; set; } = new List<DashboardCardViewModel>();

        // Recent activity summary
        public List<RecentActivityViewModel> RecentActivities { get; set; } = new List<RecentActivityViewModel>();
    }

    public class DashboardCardViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string CssClass { get; set; } = "primary";
        public string Description { get; set; } = string.Empty;
        public string Trend { get; set; } = string.Empty;
        public string TrendCssClass { get; set; } = string.Empty;
    }

    public class RecentActivityViewModel
    {
        public string Activity { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Type { get; set; } = string.Empty;
        public string CssClass { get; set; } = string.Empty;

        public string TimeAgo
        {
            get
            {
                var timeSpan = DateTime.Now - Timestamp;
                if (timeSpan.TotalMinutes < 1) return "Just now";
                if (timeSpan.TotalMinutes < 60) return $"{(int)timeSpan.TotalMinutes} minutes ago";
                if (timeSpan.TotalHours < 24) return $"{(int)timeSpan.TotalHours} hours ago";
                if (timeSpan.TotalDays < 7) return $"{(int)timeSpan.TotalDays} days ago";
                return Timestamp.ToString("MMM dd, yyyy");
            }
        }
    }

    public class RevenueReportViewModel
    {
        public Guid Id { get; set; }

        [Display(Name = "Month")]
        public int Month { get; set; }

        [Display(Name = "Year")]
        public int Year { get; set; }

        [Display(Name = "Subscription Revenue")]
        [DataType(DataType.Currency)]
        public decimal TotalSubscriptionRevenue { get; set; }

        [Display(Name = "Order Revenue")]
        [DataType(DataType.Currency)]
        public decimal TotalOrderRevenue { get; set; }

        [Display(Name = "Total Orders")]
        public int TotalOrdersCount { get; set; }

        [Display(Name = "Total Revenue")]
        [DataType(DataType.Currency)]
        public decimal TotalRevenue => TotalSubscriptionRevenue + TotalOrderRevenue;

        [Display(Name = "Average Order Value")]
        [DataType(DataType.Currency)]
        public decimal AverageOrderValue => TotalOrdersCount > 0 ? TotalOrderRevenue / TotalOrdersCount : 0;

        // Display properties
        public string MonthName => new DateTime(Year, Month, 1).ToString("MMMM");
        public string MonthYearDisplay => $"{MonthName} {Year}";
        public string ReportPeriod => MonthYearDisplay;

        // Revenue breakdown percentages
        public decimal SubscriptionRevenuePercentage => TotalRevenue > 0 ? (TotalSubscriptionRevenue / TotalRevenue) * 100 : 0;
        public decimal OrderRevenuePercentage => TotalRevenue > 0 ? (TotalOrderRevenue / TotalRevenue) * 100 : 0;

        // Status indicators
        public bool HasSubscriptionRevenue => TotalSubscriptionRevenue > 0;
        public bool HasOrderRevenue => TotalOrderRevenue > 0;
        public bool HasOrders => TotalOrdersCount > 0;
    }

    public class RevenueReportsListViewModel
    {
        public List<RevenueReportViewModel> MonthlyReports { get; set; } = new List<RevenueReportViewModel>();
        public YearlyRevenueSummaryViewModel YearlySummary { get; set; } = new YearlyRevenueSummaryViewModel();

        [Display(Name = "Selected Year")]
        public int SelectedYear { get; set; } = DateTime.Now.Year;

        public List<int> AvailableYears { get; set; } = new List<int>();

        // Computed properties
        public bool HasReports => MonthlyReports.Any();
        public decimal TotalYearRevenue => MonthlyReports.Sum(r => r.TotalRevenue);
        public int TotalYearOrders => MonthlyReports.Sum(r => r.TotalOrdersCount);
        public decimal AverageMonthlyRevenue => MonthlyReports.Any() ? MonthlyReports.Average(r => r.TotalRevenue) : 0;

        // Chart data for visualization
        public List<MonthlyRevenueChartData> ChartData { get; set; } = new List<MonthlyRevenueChartData>();
    }

    public class YearlyRevenueSummaryViewModel
    {
        [Display(Name = "Year")]
        public int Year { get; set; }

        [Display(Name = "Total Revenue")]
        [DataType(DataType.Currency)]
        public decimal TotalRevenue { get; set; }

        [Display(Name = "Total Subscription Revenue")]
        [DataType(DataType.Currency)]
        public decimal TotalSubscriptionRevenue { get; set; }

        [Display(Name = "Total Order Revenue")]
        [DataType(DataType.Currency)]
        public decimal TotalOrderRevenue { get; set; }

        [Display(Name = "Total Orders")]
        public int TotalOrders { get; set; }

        [Display(Name = "Average Monthly Revenue")]
        [DataType(DataType.Currency)]
        public decimal AverageMonthlyRevenue { get; set; }

        [Display(Name = "Best Month")]
        public string BestMonth { get; set; } = string.Empty;

        [Display(Name = "Best Month Revenue")]
        [DataType(DataType.Currency)]
        public decimal BestMonthRevenue { get; set; }

        // Growth indicators
        public decimal GrowthPercentage { get; set; }
        public string GrowthIndicator => GrowthPercentage > 0 ? "up" : GrowthPercentage < 0 ? "down" : "stable";
        public string GrowthCssClass => GrowthIndicator switch
        {
            "up" => "text-success",
            "down" => "text-danger",
            _ => "text-muted"
        };
    }

    public class MonthlyRevenueChartData
    {
        public string Month { get; set; } = string.Empty;
        public decimal SubscriptionRevenue { get; set; }
        public decimal OrderRevenue { get; set; }
        public decimal TotalRevenue { get; set; }
        public int OrderCount { get; set; }
    }

    public class AIConfigurationViewModel
    {
        [Display(Name = "Current AI Model Version")]
        public string CurrentModelVersion { get; set; } = string.Empty;

        [Display(Name = "Last Updated")]
        [DataType(DataType.DateTime)]
        public DateTime LastUpdated { get; set; }

        [Display(Name = "AI Features Enabled")]
        public bool AIFeaturesEnabled { get; set; }

        [Display(Name = "Meal Plan Generation Enabled")]
        public bool MealPlanGenerationEnabled { get; set; }

        [Display(Name = "Nutrition Calculation Enabled")]
        public bool NutritionCalculationEnabled { get; set; }

        [Display(Name = "Price Adjustment Enabled")]
        public bool PriceAdjustmentEnabled { get; set; }

        // Configuration options
        [Display(Name = "New Model Version")]
        [StringLength(50, ErrorMessage = "Model version cannot exceed 50 characters")]
        public string NewModelVersion { get; set; } = string.Empty;

        [Display(Name = "Configuration Notes")]
        [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
        public string ConfigurationNotes { get; set; } = string.Empty;

        // Status indicators
        public string StatusIndicator => AIFeaturesEnabled ? "Active" : "Inactive";
        public string StatusCssClass => AIFeaturesEnabled ? "text-success" : "text-warning";

        // AI operation logs (stub for now)
        public List<AIOperationLogViewModel> RecentOperations { get; set; } = new List<AIOperationLogViewModel>();
    }

    public class AIOperationLogViewModel
    {
        public DateTime Timestamp { get; set; }
        public string Operation { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;

        public string StatusCssClass => Status switch
        {
            "Success" => "text-success",
            "Failed" => "text-danger",
            "Warning" => "text-warning",
            _ => "text-muted"
        };

        public string TimeAgo
        {
            get
            {
                var timeSpan = DateTime.Now - Timestamp;
                if (timeSpan.TotalMinutes < 1) return "Just now";
                if (timeSpan.TotalMinutes < 60) return $"{(int)timeSpan.TotalMinutes} minutes ago";
                if (timeSpan.TotalHours < 24) return $"{(int)timeSpan.TotalHours} hours ago";
                return Timestamp.ToString("MMM dd, yyyy");
            }
        }
    }

    public class UpdateAIConfigurationViewModel
    {
        [Required(ErrorMessage = "Model version is required")]
        [Display(Name = "AI Model Version")]
        [StringLength(50, ErrorMessage = "Model version cannot exceed 50 characters")]
        public string ModelVersion { get; set; } = string.Empty;

        [Display(Name = "Enable AI Features")]
        public bool EnableAIFeatures { get; set; }

        [Display(Name = "Enable Meal Plan Generation")]
        public bool EnableMealPlanGeneration { get; set; }

        [Display(Name = "Enable Nutrition Calculation")]
        public bool EnableNutritionCalculation { get; set; }

        [Display(Name = "Enable Price Adjustment")]
        public bool EnablePriceAdjustment { get; set; }

        [Display(Name = "Configuration Notes")]
        [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
        public string Notes { get; set; } = string.Empty;
    }

    // Staff Account Management ViewModels
    public class StaffAccountsListViewModel
    {
        public List<StaffAccountViewModel> Accounts { get; set; } = new();
        public string? FilterRole { get; set; }
        
        public int TotalStaff => Accounts.Count;
        public int TotalManagers => Accounts.Count(a => a.Role == "Manager");
        public int TotalDeliveryMen => Accounts.Count(a => a.Role == "DeliveryMan");
    }

    public class StaffAccountViewModel
    {
        public Guid Id { get; set; }
        
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;
        
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;
        
        [Display(Name = "Role")]
        public string Role { get; set; } = string.Empty;
        
        public string RoleBadgeClass => Role switch
        {
            "Manager" => "badge bg-primary",
            "DeliveryMan" => "badge bg-info",
            _ => "badge bg-secondary"
        };
    }

    public class CreateStaffAccountViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm password is required")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role is required")]
        [Display(Name = "Role")]
        public string Role { get; set; } = "Manager";
    }

    public class EditStaffAccountViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        [DataType(DataType.Password)]
        [Display(Name = "New Password (leave blank to keep current)")]
        public string? Password { get; set; }

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        [Display(Name = "Confirm New Password")]
        public string? ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Role is required")]
        [Display(Name = "Role")]
        public string Role { get; set; } = string.Empty;
    }
}
