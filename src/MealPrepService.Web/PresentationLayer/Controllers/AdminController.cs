using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;
using MealPrepService.Web.PresentationLayer.ViewModels;

namespace MealPrepService.Web.PresentationLayer.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IRevenueService _revenueService;
        private readonly IAccountService _accountService;
        private readonly ISystemConfigurationService _systemConfigService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            IRevenueService revenueService,
            IAccountService accountService,
            ISystemConfigurationService systemConfigService,
            ILogger<AdminController> logger)
        {
            _revenueService = revenueService ?? throw new ArgumentNullException(nameof(revenueService));
            _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
            _systemConfigService = systemConfigService ?? throw new ArgumentNullException(nameof(systemConfigService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET: Admin/Dashboard - Show admin dashboard with statistics
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var dashboardStats = await _revenueService.GetDashboardStatsAsync();
                
                var viewModel = new AdminDashboardViewModel
                {
                    TotalCustomers = dashboardStats.TotalCustomers,
                    ActiveSubscriptions = dashboardStats.ActiveSubscriptions,
                    PendingOrders = dashboardStats.PendingOrders,
                    CurrentMonthRevenue = dashboardStats.CurrentMonthRevenue,
                    LastUpdated = DateTime.Now
                };

                // Create dashboard cards for better visualization
                viewModel.DashboardCards = new List<DashboardCardViewModel>
                {
                    new DashboardCardViewModel
                    {
                        Title = "Total Customers",
                        Value = dashboardStats.TotalCustomers.ToString(),
                        Icon = "fas fa-users",
                        CssClass = "primary",
                        Description = "Registered customers"
                    },
                    new DashboardCardViewModel
                    {
                        Title = "Active Subscriptions",
                        Value = dashboardStats.ActiveSubscriptions.ToString(),
                        Icon = "fas fa-crown",
                        CssClass = "success",
                        Description = "Currently active subscriptions"
                    },
                    new DashboardCardViewModel
                    {
                        Title = "Pending Orders",
                        Value = dashboardStats.PendingOrders.ToString(),
                        Icon = "fas fa-shopping-cart",
                        CssClass = dashboardStats.PendingOrders > 0 ? "warning" : "info",
                        Description = "Orders awaiting processing"
                    },
                    new DashboardCardViewModel
                    {
                        Title = "Monthly Revenue",
                        Value = dashboardStats.CurrentMonthRevenue.ToString("C"),
                        Icon = "fas fa-dollar-sign",
                        CssClass = "info",
                        Description = $"Revenue for {DateTime.Now:MMMM yyyy}"
                    }
                };

                // Add some sample recent activities (stub implementation)
                viewModel.RecentActivities = new List<RecentActivityViewModel>
                {
                    new RecentActivityViewModel
                    {
                        Activity = "New customer registration",
                        Timestamp = DateTime.Now.AddMinutes(-15),
                        Type = "customer",
                        CssClass = "text-success"
                    },
                    new RecentActivityViewModel
                    {
                        Activity = "Order placed",
                        Timestamp = DateTime.Now.AddMinutes(-30),
                        Type = "order",
                        CssClass = "text-info"
                    },
                    new RecentActivityViewModel
                    {
                        Activity = "Subscription renewed",
                        Timestamp = DateTime.Now.AddHours(-2),
                        Type = "subscription",
                        CssClass = "text-primary"
                    }
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading admin dashboard");
                TempData["ErrorMessage"] = "An error occurred while loading the dashboard.";
                return View(new AdminDashboardViewModel());
            }
        }

        // GET: Admin/Revenue - Show revenue reports
        [HttpGet]
        public async Task<IActionResult> Revenue(int? year)
        {
            try
            {
                var selectedYear = year ?? DateTime.Now.Year;
                var monthlyReports = new List<RevenueReportViewModel>();
                
                // Get monthly reports for the selected year
                for (int month = 1; month <= 12; month++)
                {
                    try
                    {
                        var report = await _revenueService.GetMonthlyReportAsync(selectedYear, month);
                        if (report != null)
                        {
                            monthlyReports.Add(MapToRevenueReportViewModel(report));
                        }
                    }
                    catch (BusinessException)
                    {
                        // Skip months without reports
                        continue;
                    }
                }

                // Get yearly revenue summary
                var yearlyRevenue = await _revenueService.GetYearlyRevenueAsync(selectedYear);
                
                var yearlySummary = new YearlyRevenueSummaryViewModel
                {
                    Year = selectedYear,
                    TotalRevenue = yearlyRevenue,
                    TotalSubscriptionRevenue = monthlyReports.Sum(r => r.TotalSubscriptionRevenue),
                    TotalOrderRevenue = monthlyReports.Sum(r => r.TotalOrderRevenue),
                    TotalOrders = monthlyReports.Sum(r => r.TotalOrdersCount),
                    AverageMonthlyRevenue = monthlyReports.Any() ? monthlyReports.Average(r => r.TotalRevenue) : 0
                };

                // Find best performing month
                var bestMonth = monthlyReports.OrderByDescending(r => r.TotalRevenue).FirstOrDefault();
                if (bestMonth != null)
                {
                    yearlySummary.BestMonth = bestMonth.MonthName;
                    yearlySummary.BestMonthRevenue = bestMonth.TotalRevenue;
                }

                // Create chart data
                var chartData = monthlyReports.Select(r => new MonthlyRevenueChartData
                {
                    Month = r.MonthName,
                    SubscriptionRevenue = r.TotalSubscriptionRevenue,
                    OrderRevenue = r.TotalOrderRevenue,
                    TotalRevenue = r.TotalRevenue,
                    OrderCount = r.TotalOrdersCount
                }).ToList();

                var viewModel = new RevenueReportsListViewModel
                {
                    MonthlyReports = monthlyReports.OrderBy(r => r.Month).ToList(),
                    YearlySummary = yearlySummary,
                    SelectedYear = selectedYear,
                    AvailableYears = GetAvailableYears(),
                    ChartData = chartData
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading revenue reports for year {Year}", year);
                TempData["ErrorMessage"] = "An error occurred while loading revenue reports.";
                return View(new RevenueReportsListViewModel { SelectedYear = year ?? DateTime.Now.Year });
            }
        }

        // POST: Admin/GenerateMonthlyReport - Generate monthly revenue report
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateMonthlyReport(int year, int month)
        {
            try
            {
                var report = await _revenueService.GenerateMonthlyReportAsync(year, month);
                
                TempData["SuccessMessage"] = $"Monthly report for {new DateTime(year, month, 1):MMMM yyyy} generated successfully.";
                _logger.LogInformation("Monthly revenue report generated for {Year}-{Month} by admin {AdminId}", 
                    year, month, GetCurrentAccountId());
                
                return RedirectToAction(nameof(Revenue), new { year });
            }
            catch (BusinessException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                _logger.LogWarning(ex, "Business error generating monthly report for {Year}-{Month}", year, month);
                return RedirectToAction(nameof(Revenue), new { year });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while generating the monthly report. Please try again.";
                _logger.LogError(ex, "Unexpected error generating monthly report for {Year}-{Month}", year, month);
                return RedirectToAction(nameof(Revenue), new { year });
            }
        }

        // POST: Admin/AdjustSubscriptionPrices - Adjust subscription prices using AI (stub)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AdjustSubscriptionPrices()
        {
            try
            {
                // Stub implementation - in real implementation, this would use AI to adjust prices
                TempData["SuccessMessage"] = "Subscription prices adjusted successfully using AI recommendations.";
                _logger.LogInformation("Subscription prices adjusted using AI by admin {AdminId}", GetCurrentAccountId());
                
                return RedirectToAction(nameof(Dashboard));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while adjusting subscription prices. Please try again.";
                _logger.LogError(ex, "Unexpected error adjusting subscription prices");
                return RedirectToAction(nameof(Dashboard));
            }
        }

        #region Staff Account Management

        // GET: Admin/StaffAccounts - List all Manager and DeliveryMan accounts
        [HttpGet]
        public async Task<IActionResult> StaffAccounts(string? role)
        {
            try
            {
                IEnumerable<AccountDto> accounts;
                
                if (!string.IsNullOrWhiteSpace(role) && (role == "Manager" || role == "DeliveryMan"))
                {
                    accounts = await _accountService.GetAccountsByRoleAsync(role);
                }
                else
                {
                    accounts = await _accountService.GetAllStaffAccountsAsync();
                }

                var viewModel = new StaffAccountsListViewModel
                {
                    Accounts = accounts.Select(a => new StaffAccountViewModel
                    {
                        Id = a.Id,
                        Email = a.Email,
                        FullName = a.FullName,
                        Role = a.Role
                    }).ToList(),
                    FilterRole = role
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading staff accounts");
                TempData["ErrorMessage"] = "An error occurred while loading staff accounts.";
                return View(new StaffAccountsListViewModel());
            }
        }

        // GET: Admin/CreateStaffAccount - Show create staff account form
        [HttpGet]
        public IActionResult CreateStaffAccount()
        {
            return View(new CreateStaffAccountViewModel());
        }

        // POST: Admin/CreateStaffAccount - Create new Manager or DeliveryMan account
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStaffAccount(CreateStaffAccountViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var dto = new CreateAccountDto
                {
                    Email = model.Email,
                    Password = model.Password,
                    FullName = model.FullName
                };

                var account = await _accountService.CreateStaffAccountAsync(dto, model.Role);
                
                TempData["SuccessMessage"] = $"{model.Role} account created successfully for {account.FullName}.";
                _logger.LogInformation("{Role} account created by admin {AdminId} for {Email}", 
                    model.Role, GetCurrentAccountId(), account.Email);
                
                return RedirectToAction(nameof(StaffAccounts));
            }
            catch (BusinessException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                _logger.LogWarning(ex, "Business error creating staff account");
                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "An error occurred while creating the account. Please try again.");
                _logger.LogError(ex, "Unexpected error creating staff account");
                return View(model);
            }
        }

        // GET: Admin/EditStaffAccount/{id} - Show edit staff account form
        [HttpGet]
        public async Task<IActionResult> EditStaffAccount(Guid id)
        {
            try
            {
                var account = await _accountService.GetByIdAsync(id);
                
                // Validate it's a staff account
                if (account.Role != "Manager" && account.Role != "DeliveryMan")
                {
                    TempData["ErrorMessage"] = "Only Manager and DeliveryMan accounts can be edited.";
                    return RedirectToAction(nameof(StaffAccounts));
                }

                var viewModel = new EditStaffAccountViewModel
                {
                    Id = account.Id,
                    Email = account.Email,
                    FullName = account.FullName,
                    Role = account.Role
                };

                return View(viewModel);
            }
            catch (BusinessException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(StaffAccounts));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading staff account for edit");
                TempData["ErrorMessage"] = "An error occurred while loading the account.";
                return RedirectToAction(nameof(StaffAccounts));
            }
        }

        // POST: Admin/EditStaffAccount - Update Manager or DeliveryMan account
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditStaffAccount(EditStaffAccountViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var dto = new UpdateAccountDto
                {
                    Email = model.Email,
                    FullName = model.FullName,
                    Role = model.Role,
                    Password = model.Password // Only update if provided
                };

                var account = await _accountService.UpdateStaffAccountAsync(model.Id, dto);
                
                TempData["SuccessMessage"] = $"Staff account updated successfully for {account.FullName}.";
                _logger.LogInformation("Staff account {AccountId} updated by admin {AdminId}", 
                    model.Id, GetCurrentAccountId());
                
                return RedirectToAction(nameof(StaffAccounts));
            }
            catch (BusinessException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                _logger.LogWarning(ex, "Business error updating staff account");
                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "An error occurred while updating the account. Please try again.");
                _logger.LogError(ex, "Unexpected error updating staff account");
                return View(model);
            }
        }

        // GET: Admin/DeleteStaffAccount/{id} - Show delete confirmation
        [HttpGet]
        public async Task<IActionResult> DeleteStaffAccount(Guid id)
        {
            try
            {
                var account = await _accountService.GetByIdAsync(id);
                
                // Validate it's a staff account
                if (account.Role != "Manager" && account.Role != "DeliveryMan")
                {
                    TempData["ErrorMessage"] = "Only Manager and DeliveryMan accounts can be deleted.";
                    return RedirectToAction(nameof(StaffAccounts));
                }

                var viewModel = new StaffAccountViewModel
                {
                    Id = account.Id,
                    Email = account.Email,
                    FullName = account.FullName,
                    Role = account.Role
                };

                return View(viewModel);
            }
            catch (BusinessException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(StaffAccounts));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading staff account for deletion");
                TempData["ErrorMessage"] = "An error occurred while loading the account.";
                return RedirectToAction(nameof(StaffAccounts));
            }
        }

        // POST: Admin/DeleteStaffAccountConfirmed - Delete Manager or DeliveryMan account
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteStaffAccountConfirmed(Guid id)
        {
            try
            {
                var account = await _accountService.GetByIdAsync(id);
                var accountName = account.FullName;
                var accountRole = account.Role;
                
                await _accountService.DeleteStaffAccountAsync(id);
                
                TempData["SuccessMessage"] = $"{accountRole} account for {accountName} deleted successfully.";
                _logger.LogInformation("Staff account {AccountId} deleted by admin {AdminId}", 
                    id, GetCurrentAccountId());
                
                return RedirectToAction(nameof(StaffAccounts));
            }
            catch (BusinessException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                _logger.LogWarning(ex, "Business error deleting staff account");
                return RedirectToAction(nameof(StaffAccounts));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while deleting the account. Please try again.";
                _logger.LogError(ex, "Unexpected error deleting staff account");
                return RedirectToAction(nameof(StaffAccounts));
            }
        }

        #endregion

        #region System Configuration

        // GET: Admin/SystemConfiguration - Show system configuration page
        [HttpGet]
        public async Task<IActionResult> SystemConfiguration()
        {
            try
            {
                var maxMealPlans = await _systemConfigService.GetMaxMealPlansPerCustomerAsync();
                var maxFridgeItems = await _systemConfigService.GetMaxFridgeItemsPerCustomerAsync();
                var maxMealPlanDays = await _systemConfigService.GetMaxMealPlanDaysAsync();

                var viewModel = new SystemConfigurationViewModel
                {
                    MaxMealPlansPerCustomer = maxMealPlans,
                    MaxFridgeItemsPerCustomer = maxFridgeItems,
                    MaxMealPlanDays = maxMealPlanDays
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading system configuration");
                TempData["ErrorMessage"] = "An error occurred while loading system configuration.";
                return RedirectToAction(nameof(Dashboard));
            }
        }

        // POST: Admin/SystemConfiguration - Update system configuration
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SystemConfiguration(SystemConfigurationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var adminEmail = User.Identity?.Name ?? "Admin";

                await _systemConfigService.UpdateMaxMealPlansAsync(model.MaxMealPlansPerCustomer, adminEmail);
                await _systemConfigService.UpdateMaxFridgeItemsAsync(model.MaxFridgeItemsPerCustomer, adminEmail);
                await _systemConfigService.UpdateMaxMealPlanDaysAsync(model.MaxMealPlanDays, adminEmail);

                TempData["SuccessMessage"] = "System configuration updated successfully!";
                _logger.LogInformation("System configuration updated by {AdminEmail}: MaxMealPlans={MaxMealPlans}, MaxFridgeItems={MaxFridgeItems}, MaxMealPlanDays={MaxMealPlanDays}",
                    adminEmail, model.MaxMealPlansPerCustomer, model.MaxFridgeItemsPerCustomer, model.MaxMealPlanDays);

                return RedirectToAction(nameof(SystemConfiguration));
            }
            catch (BusinessException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                _logger.LogWarning(ex, "Business error updating system configuration");
                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "An error occurred while updating system configuration. Please try again.");
                _logger.LogError(ex, "Unexpected error updating system configuration");
                return View(model);
            }
        }

        #endregion

        #region Private Helper Methods

        private Guid GetCurrentAccountId()
        {
            var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountIdClaim) || !Guid.TryParse(accountIdClaim, out var accountId))
            {
                throw new AuthenticationException("User account ID not found in claims.");
            }
            return accountId;
        }

        private RevenueReportViewModel MapToRevenueReportViewModel(RevenueReportDto dto)
        {
            return new RevenueReportViewModel
            {
                Id = dto.Id,
                Month = dto.Month,
                Year = dto.Year,
                TotalSubscriptionRevenue = dto.TotalSubscriptionRevenue,
                TotalOrderRevenue = dto.TotalOrderRevenue,
                TotalOrdersCount = dto.TotalOrdersCount
            };
        }

        private List<int> GetAvailableYears()
        {
            // Return last 3 years and current year
            var currentYear = DateTime.Now.Year;
            return new List<int> { currentYear - 2, currentYear - 1, currentYear };
        }

        #endregion
    }
}