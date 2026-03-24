using MealPreparationService.Domain.Services;
using MealPreparationService.DataAccess.UnitOfWork;
using MealPreparationService.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace MealPreparationService.DataAccess.Data;

/// <summary>
/// Service responsible for seeding initial data into the database.
/// </summary>
public class DatabaseSeeder
{
    private const string DashboardSeedMarkerKey = "seed.dashboard.synthetic.v1";

    private readonly IUnitOfWork _unitOfWork;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabaseSeeder> _logger;
    private readonly IDateTimeService _dateTimeService;

    public DatabaseSeeder(
        IUnitOfWork unitOfWork,
        ApplicationDbContext context,
        ILogger<DatabaseSeeder> logger,
        IDateTimeService dateTimeService)
    {
        _unitOfWork = unitOfWork;
        _context = context;
        _logger = logger;
        _dateTimeService = dateTimeService;
    }

    /// <summary>
    /// Seeds the database with initial required data including default admin account.
    /// </summary>
    public async Task SeedAsync()
    {
        try
        {
            await SeedAdminAccountAsync();
            await SeedDashboardSimulationDataAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }

    /// <summary>
    /// Creates a default admin account if one doesn't exist.
    /// Email: admin@mealprep.com
    /// Password: Admin123@
    /// Name: System Admin
    /// </summary>
    private async Task SeedAdminAccountAsync()
    {
        const string adminEmail = "admin@mealprep.com";
        const string adminPassword = "Admin123@";
        const string adminName = "System Admin";
        const string adminPhone = "0000000000";

        // Check if admin account already exists
        var existingAdmin = await _unitOfWork.Accounts.GetByEmailAsync(adminEmail);
        if (existingAdmin != null)
        {
            _logger.LogInformation("Admin account already exists. Skipping admin creation.");
            return;
        }

        // Get or create Admin role (RoleId = 1)
        var roles = await _unitOfWork.Roles.GetAllAsync();
        var adminRole = roles.FirstOrDefault(r => r.Id == 1 || r.Name.Equals("Admin", StringComparison.OrdinalIgnoreCase));

        if (adminRole == null)
        {
            _logger.LogWarning("Admin role not found. Creating Admin role for admin account.");
            adminRole = new Role
            {
                Id = 1,
                Name = "Admin"
            };
            await _unitOfWork.Roles.AddAsync(adminRole);
            await _unitOfWork.SaveChangesAsync();
        }

        // Create admin account with hashed password
        // Using BCrypt with work factor 12 (same as PasswordHasher service)
        var adminAccount = new Account
        {
            Id = Guid.NewGuid().ToString(),
            Email = adminEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword, workFactor: 12),
            FullName = adminName,
            PhoneNumber = string.Empty,
            RoleId = 1, // Admin role
            CurrentCredits = 0,
            CreatedAt = _dateTimeService.Now,
            UpdatedAt = _dateTimeService.Now,
            IsActive = true
        };

        await _unitOfWork.Accounts.AddAsync(adminAccount);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Default admin account created successfully. Email: {Email}", adminEmail);
    }

    private async Task SeedDashboardSimulationDataAsync()
    {
        var markerExists = await _context.SystemConfigurations
            .AsNoTracking()
            .AnyAsync(c => c.Key == DashboardSeedMarkerKey);

        if (markerExists)
        {
            _logger.LogInformation("Synthetic dashboard seed already applied. Skipping.");
            return;
        }

        var seedStartDate = new DateTime(2025, 1, 1);
        var seedEndDate = _dateTimeService.Now.Date;
        var now = _dateTimeService.Now;
        var random = new Random(20260324);

        var adminRole = await EnsureRoleAsync("Admin", 1);
        var customerRole = await EnsureRoleAsync("Customer", 2);

        var statusPending = await EnsureStatusAsync("Pending", 1);
        var statusConfirmed = await EnsureStatusAsync("Confirmed", 3);
        var statusCancelled = await EnsureStatusAsync("Cancelled", 5);
        var statusCustomerReceived = await EnsureStatusAsync("Customer_Received", 6);
        var statusPaymentFailed = await EnsureStatusAsync("Payment_Failed", 7);
        var statusCustomerRejected = await EnsureStatusAsync("Customer_Rejected", 8);

        var customers = await EnsureSyntheticCustomersAsync(customerRole.Id, seedStartDate, now, random);
        var aiPackage = await EnsureAICreditPackageAsync();
        var menuMeals = await EnsureMenuMealsForSimulationAsync(statusConfirmed.Id, seedStartDate, now, random);

        if (menuMeals.Count == 0)
        {
            _logger.LogWarning("Synthetic dashboard seed aborted because no menu meals with recipes are available.");
            return;
        }

        var adminAccountId = await _context.Accounts
            .AsNoTracking()
            .Where(a => a.RoleId == adminRole.Id)
            .Select(a => a.Id)
            .FirstOrDefaultAsync();

        var statusPool = new List<(int StatusId, int Weight)>
        {
            (statusCustomerReceived.Id, 58),
            (statusConfirmed.Id, 18),
            (statusCancelled.Id, 10),
            (statusPaymentFailed.Id, 9),
            (statusCustomerRejected.Id, 5)
        };

        var totalOrders = 0;
        var totalOrderDetails = 0;
        var totalAiMealPlans = 0;
        var totalAiTransactions = 0;

        for (var day = seedStartDate; day <= seedEndDate; day = day.AddDays(1))
        {
            var maxDailyOrders = day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? 2 : 4;
            var dailyOrders = random.Next(0, maxDailyOrders + 1);

            for (var i = 0; i < dailyOrders; i++)
            {
                var customer = customers[random.Next(customers.Count)];
                var orderStatusId = PickWeightedStatusId(statusPool, random);
                var failedLikeStatus = orderStatusId == statusCancelled.Id || orderStatusId == statusPaymentFailed.Id;
                var paymentStatusId = failedLikeStatus ? statusPaymentFailed.Id : statusConfirmed.Id;
                var payDate = day.AddHours(random.Next(7, 22)).AddMinutes(random.Next(0, 60));

                var paymentGateway = new PaymentGateway
                {
                    Id = Guid.NewGuid().ToString(),
                    StatusId = paymentStatusId,
                    TransactionNo = $"ORD-{day:yyyyMMdd}-{random.Next(100000, 999999)}",
                    BankCode = "NCB",
                    ResponseCode = failedLikeStatus ? "97" : "00",
                    PayDate = payDate
                };

                _context.PaymentGateways.Add(paymentGateway);

                var detailCount = random.Next(1, 4);
                var selectedMenuMeals = menuMeals
                    .OrderBy(_ => random.Next())
                    .Take(detailCount)
                    .ToList();

                var amount = 0m;
                var detailRows = new List<(string MenuMealId, int Quantity, decimal UnitPrice)>();

                foreach (var menuMeal in selectedMenuMeals)
                {
                    var quantity = random.Next(1, 4);
                    var unitPrice = menuMeal.Price <= 0 ? random.Next(45000, 130000) : menuMeal.Price;
                    amount += quantity * unitPrice;
                    detailRows.Add((menuMeal.Id, quantity, unitPrice));
                }

                var order = new Order
                {
                    Id = Guid.NewGuid().ToString(),
                    CustomerId = customer.Id,
                    PaymentGatewayId = paymentGateway.Id,
                    OrderConfirmedBy = adminAccountId,
                    StatusId = orderStatusId,
                    Date = payDate,
                    Amount = amount,
                    PaymentMethod = "VNPAY",
                    Address = $"{random.Next(10, 999)} Synthetic Street, Ho Chi Minh City",
                    PhoneNumber = customer.PhoneNumber,
                    CreatedAt = payDate,
                    UpdatedAt = payDate
                };

                _context.Orders.Add(order);

                foreach (var detail in detailRows)
                {
                    _context.OrderDetails.Add(new OrderDetail
                    {
                        Id = Guid.NewGuid().ToString(),
                        OrderId = order.Id,
                        MenuMealId = detail.MenuMealId,
                        Quantity = detail.Quantity,
                        UnitPrice = detail.UnitPrice,
                        CreatedAt = payDate,
                        UpdatedAt = payDate
                    });

                    totalOrderDetails++;
                }

                totalOrders++;
            }

            var dailyAiMealPlans = random.NextDouble() < 0.45 ? 0 : random.Next(1, 3);
            for (var i = 0; i < dailyAiMealPlans; i++)
            {
                var customer = customers[random.Next(customers.Count)];
                var createdAt = day.AddHours(random.Next(6, 23)).AddMinutes(random.Next(0, 60));

                _context.MealPlans.Add(new MealPlan
                {
                    Id = Guid.NewGuid().ToString(),
                    AccountId = customer.Id,
                    PlanName = $"AI Plan {createdAt:yyyyMMdd}-{i + 1}",
                    StartDate = createdAt.Date,
                    EndDate = createdAt.Date.AddDays(6),
                    IsAiGenerated = true,
                    CreatedAt = createdAt,
                    UpdatedAt = createdAt,
                    IsActive = true,
                    Age = random.Next(20, 56),
                    Weight = random.Next(50, 96),
                    Height = random.Next(150, 186),
                    Gender = random.Next(0, 2) == 0 ? "Female" : "Male",
                    HealthNote = "Synthetic dashboard usage seed",
                    CaloriesGoal = random.Next(1650, 2601)
                });

                totalAiMealPlans++;
            }

            var dailyAiTransactions = random.NextDouble() < 0.5 ? 0 : random.Next(1, 3);
            for (var i = 0; i < dailyAiTransactions; i++)
            {
                var customer = customers[random.Next(customers.Count)];
                var createdAt = day.AddHours(random.Next(7, 22)).AddMinutes(random.Next(0, 60));

                var paymentGateway = new PaymentGateway
                {
                    Id = Guid.NewGuid().ToString(),
                    StatusId = statusConfirmed.Id,
                    TransactionNo = $"AI-{day:yyyyMMdd}-{random.Next(100000, 999999)}",
                    BankCode = "VCB",
                    ResponseCode = "00",
                    PayDate = createdAt
                };

                _context.PaymentGateways.Add(paymentGateway);

                _context.AIcreditTransactions.Add(new AIcreditTransaction
                {
                    Id = Guid.NewGuid().ToString(),
                    AccountId = customer.Id,
                    AIcreditPackageId = aiPackage.Id,
                    PaymentGatewayId = paymentGateway.Id,
                    CreatedAt = createdAt
                });

                totalAiTransactions++;
            }
        }

        _context.SystemConfigurations.Add(new SystemConfiguration
        {
            Id = Guid.NewGuid().ToString(),
            Key = DashboardSeedMarkerKey,
            Value = $"Generated at {now:O}",
            DataType = "string",
            Description = "Marker to prevent duplicate synthetic dashboard data seeding",
            UpdatedAt = now
        });

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Synthetic dashboard seed completed: Customers={CustomerCount}, Orders={OrderCount}, OrderDetails={OrderDetailCount}, AIPlans={AIPlanCount}, AITransactions={AITransactionCount}",
            customers.Count,
            totalOrders,
            totalOrderDetails,
            totalAiMealPlans,
            totalAiTransactions);
    }

    private async Task<Role> EnsureRoleAsync(string roleName, int preferredId)
    {
        var existingByName = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
        if (existingByName != null)
        {
            return existingByName;
        }

        var targetId = await _context.Roles.AnyAsync(r => r.Id == preferredId)
            ? (await _context.Roles.MaxAsync(r => r.Id)) + 1
            : preferredId;

        var role = new Role
        {
            Id = targetId,
            Name = roleName
        };

        _context.Roles.Add(role);
        await _context.SaveChangesAsync();
        return role;
    }

    private async Task<Status> EnsureStatusAsync(string statusName, int preferredId)
    {
        var existingByName = await _context.Statuses.FirstOrDefaultAsync(s => s.Name == statusName);
        if (existingByName != null)
        {
            return existingByName;
        }

        var targetId = await _context.Statuses.AnyAsync(s => s.Id == preferredId)
            ? (await _context.Statuses.MaxAsync(s => s.Id)) + 1
            : preferredId;

        var status = new Status
        {
            Id = targetId,
            Name = statusName
        };

        _context.Statuses.Add(status);
        await _context.SaveChangesAsync();
        return status;
    }

    private async Task<AIcreditPackage> EnsureAICreditPackageAsync()
    {
        var existingPackage = await _context.AIcreditPackages.FirstOrDefaultAsync();
        if (existingPackage != null)
        {
            return existingPackage;
        }

        var package = new AIcreditPackage
        {
            Id = Guid.NewGuid().ToString(),
            PackageName = "Starter AI Credits",
            Price = 99000,
            CreditAmount = 100
        };

        _context.AIcreditPackages.Add(package);
        await _context.SaveChangesAsync();
        return package;
    }

    private async Task<List<Account>> EnsureSyntheticCustomersAsync(int customerRoleId, DateTime seedStartDate, DateTime now, Random random)
    {
        var existing = await _context.Accounts
            .Where(a => a.Email.StartsWith("synthetic.customer.") && a.Email.EndsWith("@mealprep.local"))
            .ToListAsync();

        const int targetCustomerCount = 80;
        if (existing.Count >= targetCustomerCount)
        {
            return existing;
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword("Synthetic123@", workFactor: 12);
        var startIndex = existing.Count + 1;

        for (var i = startIndex; i <= targetCustomerCount; i++)
        {
            var createdAt = seedStartDate.AddDays(random.Next(0, Math.Max(1, (now.Date - seedStartDate.Date).Days + 1)));
            var phoneNumber = $"09{random.Next(10000000, 99999999)}";

            _context.Accounts.Add(new Account
            {
                Id = Guid.NewGuid().ToString(),
                Email = $"synthetic.customer.{i:000}@mealprep.local",
                PasswordHash = passwordHash,
                FullName = $"Synthetic Customer {i:000}",
                PhoneNumber = phoneNumber,
                RoleId = customerRoleId,
                CurrentCredits = random.Next(0, 180),
                CreatedAt = createdAt,
                UpdatedAt = createdAt,
                IsActive = true,
                LastLoginAt = createdAt.AddDays(random.Next(0, 40))
            });
        }

        await _context.SaveChangesAsync();

        return await _context.Accounts
            .Where(a => a.Email.StartsWith("synthetic.customer.") && a.Email.EndsWith("@mealprep.local"))
            .ToListAsync();
    }

    private async Task<List<MenuMeal>> EnsureMenuMealsForSimulationAsync(int activeStatusId, DateTime seedStartDate, DateTime now, Random random)
    {
        var existingMenuMeals = await _context.MenuMeals
            .Include(mm => mm.MenuMealRecipes)
            .Where(mm => mm.MenuMealRecipes.Any())
            .ToListAsync();

        if (existingMenuMeals.Count >= 8)
        {
            return existingMenuMeals;
        }

        var mealTypes = await _context.MealTypes.OrderBy(mt => mt.Id).ToListAsync();
        if (mealTypes.Count == 0)
        {
            mealTypes = new List<MealType>
            {
                new() { Id = 1, TypeName = "Breakfast" },
                new() { Id = 2, TypeName = "Lunch" },
                new() { Id = 3, TypeName = "Dinner" }
            };

            _context.MealTypes.AddRange(mealTypes);
            await _context.SaveChangesAsync();
        }

        var recipes = await _context.Recipes.ToListAsync();
        if (recipes.Count < 8)
        {
            var recipeNames = new[]
            {
                "Grilled Chicken Caesar Salad",
                "Veggie-Packed Turkey Wrap",
                "Pan-Seared Salmon with Asparagus",
                "Creamy Rice Pudding Kheer",
                "Lemon Garlic Shrimp Bowl",
                "Tofu Quinoa Protein Bowl",
                "Herb Beef Stir Fry",
                "Avocado Egg Breakfast Toast"
            };

            foreach (var name in recipeNames)
            {
                if (recipes.Any(r => r.RecipeName == name))
                {
                    continue;
                }

                recipes.Add(new Recipe
                {
                    Id = Guid.NewGuid().ToString(),
                    RecipeName = name,
                    Instructions = "Synthetic dashboard seed recipe instructions.",
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }

            await _context.SaveChangesAsync();
            recipes = await _context.Recipes.ToListAsync();
        }

        var dailyMenu = new DailyMenu
        {
            Id = Guid.NewGuid().ToString(),
            StatusId = activeStatusId,
            MenuDate = seedStartDate,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.DailyMenus.Add(dailyMenu);

        var selectedRecipes = recipes.Take(8).ToList();
        var createdMenuMeals = new List<MenuMeal>();

        for (var i = 0; i < selectedRecipes.Count; i++)
        {
            var mealType = mealTypes[i % mealTypes.Count];
            var menuMeal = new MenuMeal
            {
                Id = Guid.NewGuid().ToString(),
                MenuId = dailyMenu.Id,
                MealTypeId = mealType.Id,
                TotalCalories = random.Next(280, 760),
                ProteinG = random.Next(18, 58),
                FatG = random.Next(9, 34),
                CarbsG = random.Next(20, 85),
                Price = random.Next(48000, 145000),
                AvailableQuantity = 500,
                CreatedAt = now,
                UpdatedAt = now
            };

            createdMenuMeals.Add(menuMeal);
            _context.MenuMeals.Add(menuMeal);
        }

        await _context.SaveChangesAsync();

        for (var i = 0; i < createdMenuMeals.Count; i++)
        {
            _context.MenuMealRecipes.Add(new MenuMealRecipe
            {
                MenuMealId = createdMenuMeals[i].Id,
                RecipeId = selectedRecipes[i].Id
            });
        }

        await _context.SaveChangesAsync();

        return await _context.MenuMeals
            .Include(mm => mm.MenuMealRecipes)
            .Where(mm => mm.MenuMealRecipes.Any())
            .ToListAsync();
    }

    private static int PickWeightedStatusId(IEnumerable<(int StatusId, int Weight)> weightedStatuses, Random random)
    {
        var total = weightedStatuses.Sum(x => x.Weight);
        var roll = random.Next(1, total + 1);
        var cumulative = 0;

        foreach (var item in weightedStatuses)
        {
            cumulative += item.Weight;
            if (roll <= cumulative)
            {
                return item.StatusId;
            }
        }

        return weightedStatuses.Last().StatusId;
    }
}

