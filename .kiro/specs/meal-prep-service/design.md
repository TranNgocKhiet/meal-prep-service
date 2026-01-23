# Design Document: Meal Prep Service Application

## Overview

The Meal Prep Service Application is a comprehensive ASP.NET Core MVC web application implementing a 3-layer architecture (Data Access Layer, Business Logic Layer, Presentation Layer). The system provides subscription-based meal planning with AI-powered meal generation, nutrition calculation, virtual fridge management, order processing, and delivery tracking.

### Key Design Principles

- **Clean Architecture**: Strict separation of concerns across three layers
- **Dependency Injection**: All cross-layer dependencies managed through DI container
- **Repository Pattern**: Data access abstraction for testability and maintainability
- **Unit of Work Pattern**: Transaction management across multiple repositories
- **DTO/ViewModel Pattern**: Data transfer objects for layer communication
- **SOLID Principles**: Single responsibility, open/closed, dependency inversion

### Technology Stack

- **Framework**: ASP.NET Core 8.0 MVC
- **ORM**: Entity Framework Core 8.0
- **Database**: SQL Server (production), SQLite (development/testing)
- **Authentication**: ASP.NET Core Identity with Google OAuth
- **Dependency Injection**: Built-in ASP.NET Core DI Container
- **Testing**: xUnit, Moq, FsCheck (property-based testing)
- **Logging**: Serilog
- **Validation**: FluentValidation

## Architecture

### Three-Layer Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Presentation Layer                        │
│  (Controllers, Views, ViewModels, Filters, Middleware)      │
└─────────────────────────────────────────────────────────────┘
                            ↓ ↑
┌─────────────────────────────────────────────────────────────┐
│                   Business Logic Layer                       │
│     (Services, Business Rules, DTOs, Interfaces)            │
└─────────────────────────────────────────────────────────────┘
                            ↓ ↑
┌─────────────────────────────────────────────────────────────┐
│                    Data Access Layer                         │
│  (DbContext, Entities, Repositories, Unit of Work)          │
└─────────────────────────────────────────────────────────────┘
                            ↓ ↑
┌─────────────────────────────────────────────────────────────┐
│                        Database                              │
│                      (SQL Server)                            │
└─────────────────────────────────────────────────────────────┘
```


### Layer Responsibilities

**Presentation Layer**:
- Handle HTTP requests and responses
- Validate user input (client-side and server-side)
- Transform DTOs to ViewModels and vice versa
- Implement authorization filters
- Render views with data
- Handle session management

**Business Logic Layer**:
- Implement business rules and validation
- Coordinate operations across multiple repositories
- Transform entities to DTOs
- Handle business exceptions
- Implement AI integration logic
- Calculate derived values (nutrition, pricing)

**Data Access Layer**:
- Manage database connections
- Execute CRUD operations
- Implement queries with LINQ
- Handle database transactions
- Map entities to database tables
- Implement repository interfaces

## Components and Interfaces

### Data Access Layer Components

#### Entity Models

All entities follow the ERD schema with the following base structure:

```csharp
// Base entity for common properties
public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

// Account entity
public class Account : BaseEntity
{
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public string FullName { get; set; }
    public string Role { get; set; } // Admin, Manager, Customer, DeliveryMan, Guest
    
    // Navigation properties
    public HealthProfile HealthProfile { get; set; }
    public ICollection<UserSubscription> Subscriptions { get; set; }
    public ICollection<MealPlan> MealPlans { get; set; }
    public ICollection<FridgeItem> FridgeItems { get; set; }
    public ICollection<Order> Orders { get; set; }
}

// HealthProfile entity
public class HealthProfile : BaseEntity
{
    public Guid AccountId { get; set; }
    public int Age { get; set; }
    public float Weight { get; set; }
    public float Height { get; set; }
    public string Gender { get; set; }
    public string HealthNotes { get; set; }
    
    // Navigation properties
    public Account Account { get; set; }
    public ICollection<Allergy> Allergies { get; set; }
    public ICollection<FoodPreference> FoodPreferences { get; set; }
}

// Recipe entity with nutrition
public class Recipe : BaseEntity
{
    public string RecipeName { get; set; }
    public string Instructions { get; set; }
    public float TotalCalories { get; set; }
    public float ProteinG { get; set; }
    public float FatG { get; set; }
    public float CarbsG { get; set; }
    
    // Navigation properties
    public ICollection<RecipeIngredient> RecipeIngredients { get; set; }
    public ICollection<MealRecipe> MealRecipes { get; set; }
    public ICollection<MenuMeal> MenuMeals { get; set; }
}

// Order entity
public class Order : BaseEntity
{
    public Guid AccountId { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; } // "COD", "VNPAY"
    public string Status { get; set; } // pending, pending_payment, paid, payment_failed, confirmed, delivered
    public string VnpayTransactionId { get; set; } // For VNPAY transactions
    public DateTime? PaymentConfirmedAt { get; set; } // When payment was confirmed
    public Guid? PaymentConfirmedBy { get; set; } // Delivery man ID for COD confirmations
    
    // Navigation properties
    public Account Account { get; set; }
    public ICollection<OrderDetail> OrderDetails { get; set; }
    public DeliverySchedule DeliverySchedule { get; set; }
}
```

#### DbContext Configuration

```csharp
public class MealPrepDbContext : DbContext
{
    public MealPrepDbContext(DbContextOptions<MealPrepDbContext> options) 
        : base(options) { }
    
    // DbSets for all entities
    public DbSet<Account> Accounts { get; set; }
    public DbSet<HealthProfile> HealthProfiles { get; set; }
    public DbSet<Allergy> Allergies { get; set; }
    public DbSet<FoodPreference> FoodPreferences { get; set; }
    public DbSet<SubscriptionPackage> SubscriptionPackages { get; set; }
    public DbSet<UserSubscription> UserSubscriptions { get; set; }
    public DbSet<MealPlan> MealPlans { get; set; }
    public DbSet<Meal> Meals { get; set; }
    public DbSet<Recipe> Recipes { get; set; }
    public DbSet<Ingredient> Ingredients { get; set; }
    public DbSet<RecipeIngredient> RecipeIngredients { get; set; }
    public DbSet<FridgeItem> FridgeItems { get; set; }
    public DbSet<DailyMenu> DailyMenus { get; set; }
    public DbSet<MenuMeal> MenuMeals { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderDetail> OrderDetails { get; set; }
    public DbSet<DeliverySchedule> DeliverySchedules { get; set; }
    public DbSet<RevenueReport> RevenueReports { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure relationships, indexes, and constraints
        ConfigureAccountRelationships(modelBuilder);
        ConfigureRecipeRelationships(modelBuilder);
        ConfigureOrderRelationships(modelBuilder);
        ConfigureIndexes(modelBuilder);
        ConfigureConstraints(modelBuilder);
    }
    
    private void ConfigureAccountRelationships(ModelBuilder modelBuilder)
    {
        // One-to-one: Account to HealthProfile
        modelBuilder.Entity<Account>()
            .HasOne(a => a.HealthProfile)
            .WithOne(h => h.Account)
            .HasForeignKey<HealthProfile>(h => h.AccountId);
        
        // Many-to-many: HealthProfile to Allergies
        modelBuilder.Entity<HealthProfile>()
            .HasMany(h => h.Allergies)
            .WithMany(a => a.HealthProfiles)
            .UsingEntity(j => j.ToTable("HealthProfileAllergies"));
        
        // Many-to-many: HealthProfile to FoodPreferences
        modelBuilder.Entity<HealthProfile>()
            .HasMany(h => h.FoodPreferences)
            .WithMany(f => f.HealthProfiles)
            .UsingEntity(j => j.ToTable("HealthProfileFoodPreferences"));
    }
    
    private void ConfigureRecipeRelationships(ModelBuilder modelBuilder)
    {
        // Composite key for RecipeIngredient
        modelBuilder.Entity<RecipeIngredient>()
            .HasKey(ri => new { ri.RecipeId, ri.IngredientId });
        
        // Many-to-many: Recipe to Ingredient through RecipeIngredient
        modelBuilder.Entity<RecipeIngredient>()
            .HasOne(ri => ri.Recipe)
            .WithMany(r => r.RecipeIngredients)
            .HasForeignKey(ri => ri.RecipeId);
        
        modelBuilder.Entity<RecipeIngredient>()
            .HasOne(ri => ri.Ingredient)
            .WithMany(i => i.RecipeIngredients)
            .HasForeignKey(ri => ri.IngredientId);
    }
    
    private void ConfigureOrderRelationships(ModelBuilder modelBuilder)
    {
        // One-to-one: Order to DeliverySchedule
        modelBuilder.Entity<Order>()
            .HasOne(o => o.DeliverySchedule)
            .WithOne(d => d.Order)
            .HasForeignKey<DeliverySchedule>(d => d.OrderId);
    }
    
    private void ConfigureIndexes(ModelBuilder modelBuilder)
    {
        // Unique index on Account.Email
        modelBuilder.Entity<Account>()
            .HasIndex(a => a.Email)
            .IsUnique();
        
        // Index on UserSubscription for active subscription queries
        modelBuilder.Entity<UserSubscription>()
            .HasIndex(us => new { us.AccountId, us.Status, us.EndDate });
        
        // Index on DailyMenu.MenuDate for date-based queries
        modelBuilder.Entity<DailyMenu>()
            .HasIndex(dm => dm.MenuDate);
        
        // Index on Order.OrderDate for reporting
        modelBuilder.Entity<Order>()
            .HasIndex(o => o.OrderDate);
    }
    
    private void ConfigureConstraints(ModelBuilder modelBuilder)
    {
        // Email validation constraint
        modelBuilder.Entity<Account>()
            .Property(a => a.Email)
            .IsRequired()
            .HasMaxLength(255);
        
        // Positive value constraints
        modelBuilder.Entity<HealthProfile>()
            .Property(h => h.Weight)
            .HasPrecision(5, 2);
        
        modelBuilder.Entity<HealthProfile>()
            .Property(h => h.Height)
            .HasPrecision(5, 2);
        
        // Decimal precision for money
        modelBuilder.Entity<SubscriptionPackage>()
            .Property(sp => sp.Price)
            .HasPrecision(10, 2);
        
        modelBuilder.Entity<Order>()
            .Property(o => o.TotalAmount)
            .HasPrecision(10, 2);
    }
}
```


#### Repository Interfaces

```csharp
// Generic repository interface
public interface IRepository<T> where T : BaseEntity
{
    Task<T> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
}

// Specialized repository interfaces
public interface IAccountRepository : IRepository<Account>
{
    Task<Account> GetByEmailAsync(string email);
    Task<bool> EmailExistsAsync(string email);
    Task<Account> GetWithHealthProfileAsync(Guid accountId);
}

public interface IUserSubscriptionRepository : IRepository<UserSubscription>
{
    Task<UserSubscription> GetActiveSubscriptionAsync(Guid accountId);
    Task<IEnumerable<UserSubscription>> GetExpiredSubscriptionsAsync();
    Task<int> GetActiveSubscriptionCountAsync();
}

public interface IMealPlanRepository : IRepository<MealPlan>
{
    Task<IEnumerable<MealPlan>> GetByAccountIdAsync(Guid accountId);
    Task<MealPlan> GetWithMealsAndRecipesAsync(Guid planId);
}

public interface IRecipeRepository : IRepository<Recipe>
{
    Task<IEnumerable<Recipe>> GetByIngredientsAsync(IEnumerable<Guid> ingredientIds);
    Task<IEnumerable<Recipe>> GetExcludingAllergensAsync(IEnumerable<Guid> allergyIds);
    Task<bool> IsUsedInActiveMenuAsync(Guid recipeId);
}

public interface IOrderRepository : IRepository<Order>
{
    Task<IEnumerable<Order>> GetByAccountIdAsync(Guid accountId);
    Task<IEnumerable<Order>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<Order> GetWithDetailsAsync(Guid orderId);
    Task<decimal> GetTotalRevenueByMonthAsync(int year, int month);
}

public interface IDailyMenuRepository : IRepository<DailyMenu>
{
    Task<DailyMenu> GetByDateAsync(DateTime date);
    Task<IEnumerable<DailyMenu>> GetWeeklyMenuAsync(DateTime startDate);
    Task<DailyMenu> GetWithMealsAsync(Guid menuId);
}

public interface IFridgeItemRepository : IRepository<FridgeItem>
{
    Task<IEnumerable<FridgeItem>> GetByAccountIdAsync(Guid accountId);
    Task<IEnumerable<FridgeItem>> GetExpiringItemsAsync(Guid accountId, int daysThreshold);
}
```

#### Unit of Work Pattern

```csharp
public interface IUnitOfWork : IDisposable
{
    IAccountRepository Accounts { get; }
    IUserSubscriptionRepository UserSubscriptions { get; }
    IMealPlanRepository MealPlans { get; }
    IRecipeRepository Recipes { get; }
    IOrderRepository Orders { get; }
    IDailyMenuRepository DailyMenus { get; }
    IFridgeItemRepository FridgeItems { get; }
    IRepository<HealthProfile> HealthProfiles { get; }
    IRepository<Ingredient> Ingredients { get; }
    IRepository<MenuMeal> MenuMeals { get; }
    IRepository<DeliverySchedule> DeliverySchedules { get; }
    IRepository<RevenueReport> RevenueReports { get; }
    
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}

public class UnitOfWork : IUnitOfWork
{
    private readonly MealPrepDbContext _context;
    private IDbContextTransaction _transaction;
    
    public UnitOfWork(MealPrepDbContext context)
    {
        _context = context;
        
        // Initialize repositories
        Accounts = new AccountRepository(_context);
        UserSubscriptions = new UserSubscriptionRepository(_context);
        MealPlans = new MealPlanRepository(_context);
        Recipes = new RecipeRepository(_context);
        Orders = new OrderRepository(_context);
        DailyMenus = new DailyMenuRepository(_context);
        FridgeItems = new FridgeItemRepository(_context);
        HealthProfiles = new Repository<HealthProfile>(_context);
        Ingredients = new Repository<Ingredient>(_context);
        MenuMeals = new Repository<MenuMeal>(_context);
        DeliverySchedules = new Repository<DeliverySchedule>(_context);
        RevenueReports = new Repository<RevenueReport>(_context);
    }
    
    public IAccountRepository Accounts { get; private set; }
    public IUserSubscriptionRepository UserSubscriptions { get; private set; }
    public IMealPlanRepository MealPlans { get; private set; }
    public IRecipeRepository Recipes { get; private set; }
    public IOrderRepository Orders { get; private set; }
    public IDailyMenuRepository DailyMenus { get; private set; }
    public IFridgeItemRepository FridgeItems { get; private set; }
    public IRepository<HealthProfile> HealthProfiles { get; private set; }
    public IRepository<Ingredient> Ingredients { get; private set; }
    public IRepository<MenuMeal> MenuMeals { get; private set; }
    public IRepository<DeliverySchedule> DeliverySchedules { get; private set; }
    public IRepository<RevenueReport> RevenueReports { get; private set; }
    
    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
    
    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }
    
    public async Task CommitTransactionAsync()
    {
        await _transaction.CommitAsync();
    }
    
    public async Task RollbackTransactionAsync()
    {
        await _transaction.RollbackAsync();
    }
    
    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
```


### Business Logic Layer Components

#### Service Interfaces and DTOs

```csharp
// DTOs for data transfer
public class AccountDto
{
    public Guid Id { get; set; }
    public string Email { get; set; }
    public string FullName { get; set; }
    public string Role { get; set; }
}

public class CreateAccountDto
{
    public string Email { get; set; }
    public string Password { get; set; }
    public string FullName { get; set; }
}

public class HealthProfileDto
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public int Age { get; set; }
    public float Weight { get; set; }
    public float Height { get; set; }
    public string Gender { get; set; }
    public string HealthNotes { get; set; }
    public List<string> Allergies { get; set; }
    public List<string> FoodPreferences { get; set; }
}

public class MealPlanDto
{
    public Guid Id { get; set; }
    public string PlanName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsAiGenerated { get; set; }
    public List<MealDto> Meals { get; set; }
}

public class OrderDto
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; }
    public string Status { get; set; }
    public string VnpayTransactionId { get; set; }
    public DateTime? PaymentConfirmedAt { get; set; }
    public Guid? PaymentConfirmedBy { get; set; }
    public List<OrderDetailDto> OrderDetails { get; set; }
}

public class VnpayPaymentUrlDto
{
    public string PaymentUrl { get; set; }
    public string TransactionId { get; set; }
}

public class VnpayCallbackDto
{
    public string vnp_TmnCode { get; set; }
    public string vnp_Amount { get; set; }
    public string vnp_BankCode { get; set; }
    public string vnp_BankTranNo { get; set; }
    public string vnp_CardType { get; set; }
    public string vnp_PayDate { get; set; }
    public string vnp_OrderInfo { get; set; }
    public string vnp_TransactionNo { get; set; }
    public string vnp_ResponseCode { get; set; }
    public string vnp_TransactionStatus { get; set; }
    public string vnp_TxnRef { get; set; }
    public string vnp_SecureHashType { get; set; }
    public string vnp_SecureHash { get; set; }
}

public class VnpayCallbackResult
{
    public bool IsSuccess { get; set; }
    public string TransactionId { get; set; }
    public Guid OrderId { get; set; }
    public string ResponseCode { get; set; }
    public string Message { get; set; }
}

// Service interfaces
public interface IAccountService
{
    Task<AccountDto> RegisterAsync(CreateAccountDto dto);
    Task<AccountDto> AuthenticateAsync(string email, string password);
    Task<AccountDto> GetByIdAsync(Guid accountId);
    Task<bool> EmailExistsAsync(string email);
}

public interface IHealthProfileService
{
    Task<HealthProfileDto> CreateOrUpdateAsync(HealthProfileDto dto);
    Task<HealthProfileDto> GetByAccountIdAsync(Guid accountId);
    Task AddAllergyAsync(Guid profileId, Guid allergyId);
    Task RemoveAllergyAsync(Guid profileId, Guid allergyId);
    Task AddFoodPreferenceAsync(Guid profileId, Guid preferenceId);
    Task RemoveFoodPreferenceAsync(Guid profileId, Guid preferenceId);
}

public interface ISubscriptionService
{
    Task<IEnumerable<SubscriptionPackageDto>> GetAllPackagesAsync();
    Task<UserSubscriptionDto> SubscribeAsync(Guid accountId, Guid packageId);
    Task<UserSubscriptionDto> GetActiveSubscriptionAsync(Guid accountId);
    Task<bool> HasActiveSubscriptionAsync(Guid accountId);
    Task UpdateExpiredSubscriptionsAsync();
}

public interface IMealPlanService
{
    Task<MealPlanDto> GenerateAiMealPlanAsync(Guid accountId, DateTime startDate, DateTime endDate);
    Task<MealPlanDto> CreateManualMealPlanAsync(MealPlanDto dto);
    Task<MealPlanDto> GetByIdAsync(Guid planId);
    Task<IEnumerable<MealPlanDto>> GetByAccountIdAsync(Guid accountId);
    Task AddMealToPlannAsync(Guid planId, MealDto mealDto);
}

public interface INutritionCalculatorService
{
    Task<NutritionInfo> CalculateRecipeNutritionAsync(Guid recipeId);
    Task<NutritionInfo> CalculateMealNutritionAsync(Guid mealId);
    Task<NutritionInfo> CalculateDailyNutritionAsync(Guid planId, DateTime date);
    Task UpdateRecipeNutritionAsync(Guid recipeId);
}

public interface IFridgeService
{
    Task<IEnumerable<FridgeItemDto>> GetFridgeItemsAsync(Guid accountId);
    Task<FridgeItemDto> AddItemAsync(FridgeItemDto dto);
    Task UpdateItemQuantityAsync(Guid itemId, float newQuantity);
    Task RemoveItemAsync(Guid itemId);
    Task<IEnumerable<FridgeItemDto>> GetExpiringItemsAsync(Guid accountId);
    Task<GroceryListDto> GenerateGroceryListAsync(Guid accountId, Guid planId);
}

public interface IMenuService
{
    Task<DailyMenuDto> CreateDailyMenuAsync(DateTime menuDate);
    Task<DailyMenuDto> GetByDateAsync(DateTime date);
    Task<IEnumerable<DailyMenuDto>> GetWeeklyMenuAsync(DateTime startDate);
    Task AddMealToMenuAsync(Guid menuId, MenuMealDto menuMealDto);
    Task PublishMenuAsync(Guid menuId);
    Task UpdateMealQuantityAsync(Guid menuMealId, int newQuantity);
}

public interface IOrderService
{
    Task<OrderDto> CreateOrderAsync(Guid accountId, List<OrderItemDto> items);
    Task<OrderDto> ProcessPaymentAsync(Guid orderId, string paymentMethod);
    Task<OrderDto> ProcessVnpayCallbackAsync(VnpayCallbackDto callbackDto);
    Task<OrderDto> ConfirmCashPaymentAsync(Guid orderId, Guid deliveryManId);
    Task<OrderDto> GetByIdAsync(Guid orderId);
    Task<IEnumerable<OrderDto>> GetByAccountIdAsync(Guid accountId);
    Task UpdateOrderStatusAsync(Guid orderId, string status);
}

public interface IVnpayService
{
    Task<VnpayPaymentUrlDto> CreatePaymentUrlAsync(Guid orderId, decimal amount, string orderInfo);
    Task<VnpayCallbackResult> ProcessCallbackAsync(VnpayCallbackDto callbackDto);
    bool ValidateCallback(VnpayCallbackDto callbackDto);
}

public interface IDeliveryService
{
    Task<DeliveryScheduleDto> CreateDeliveryScheduleAsync(Guid orderId, DeliveryScheduleDto dto);
    Task<IEnumerable<DeliveryScheduleDto>> GetByAccountIdAsync(Guid accountId);
    Task<IEnumerable<DeliveryScheduleDto>> GetByDeliveryManAsync(Guid deliveryManId);
    Task CompleteDeliveryAsync(Guid deliveryId);
    Task UpdateDeliveryTimeAsync(Guid deliveryId, DateTime newTime);
}

public interface IRevenueService
{
    Task<RevenueReportDto> GenerateMonthlyReportAsync(int year, int month);
    Task<RevenueReportDto> GetMonthlyReportAsync(int year, int month);
    Task<decimal> GetYearlyRevenueAsync(int year);
    Task<DashboardStatsDto> GetDashboardStatsAsync();
}
```


#### Key Service Implementations

```csharp
// Account Service with password hashing
public class AccountService : IAccountService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<AccountService> _logger;
    
    public AccountService(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher, ILogger<AccountService> logger)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }
    
    public async Task<AccountDto> RegisterAsync(CreateAccountDto dto)
    {
        // Validate email doesn't exist
        if (await _unitOfWork.Accounts.EmailExistsAsync(dto.Email))
        {
            throw new BusinessException("Email already exists");
        }
        
        // Hash password
        var passwordHash = _passwordHasher.HashPassword(dto.Password);
        
        // Create account entity
        var account = new Account
        {
            Email = dto.Email,
            PasswordHash = passwordHash,
            FullName = dto.FullName,
            Role = "Customer",
            CreatedAt = DateTime.UtcNow
        };
        
        await _unitOfWork.Accounts.AddAsync(account);
        await _unitOfWork.SaveChangesAsync();
        
        return MapToDto(account);
    }
    
    public async Task<AccountDto> AuthenticateAsync(string email, string password)
    {
        var account = await _unitOfWork.Accounts.GetByEmailAsync(email);
        
        if (account == null || !_passwordHasher.VerifyPassword(password, account.PasswordHash))
        {
            throw new AuthenticationException("Invalid credentials");
        }
        
        return MapToDto(account);
    }
}

// Order Service with enhanced payment processing
public class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IDeliveryService _deliveryService;
    private readonly IVnpayService _vnpayService;
    private readonly ILogger<OrderService> _logger;
    
    public OrderService(
        IUnitOfWork unitOfWork, 
        ISubscriptionService subscriptionService, 
        IDeliveryService deliveryService,
        IVnpayService vnpayService,
        ILogger<OrderService> logger)
    {
        _unitOfWork = unitOfWork;
        _subscriptionService = subscriptionService;
        _deliveryService = deliveryService;
        _vnpayService = vnpayService;
        _logger = logger;
    }
    
    public async Task<OrderDto> CreateOrderAsync(Guid accountId, List<OrderItemDto> items)
    {
        // Validate active subscription
        if (!await _subscriptionService.HasActiveSubscriptionAsync(accountId))
        {
            throw new BusinessException("Active subscription required to place orders");
        }
        
        await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            // Validate menu meal availability
            var order = new Order
            {
                AccountId = accountId,
                OrderDate = DateTime.UtcNow,
                Status = "pending"
            };
            
            decimal totalAmount = 0;
            var orderDetails = new List<OrderDetail>();
            
            foreach (var item in items)
            {
                var menuMeal = await _unitOfWork.MenuMeals.GetByIdAsync(item.MenuMealId);
                
                if (menuMeal == null)
                {
                    throw new BusinessException($"Menu meal {item.MenuMealId} not found");
                }
                
                if (menuMeal.AvailableQuantity < item.Quantity)
                {
                    throw new BusinessException($"Insufficient quantity for {menuMeal.Recipe.RecipeName}");
                }
                
                // Reduce available quantity
                menuMeal.AvailableQuantity -= item.Quantity;
                await _unitOfWork.MenuMeals.UpdateAsync(menuMeal);
                
                var orderDetail = new OrderDetail
                {
                    MenuMealId = item.MenuMealId,
                    Quantity = item.Quantity,
                    UnitPrice = menuMeal.Price
                };
                
                orderDetails.Add(orderDetail);
                totalAmount += menuMeal.Price * item.Quantity;
            }
            
            order.TotalAmount = totalAmount;
            order.OrderDetails = orderDetails;
            
            await _unitOfWork.Orders.AddAsync(order);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
            
            return MapToDto(order);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }
    
    public async Task<OrderDto> ProcessPaymentAsync(Guid orderId, string paymentMethod)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
        
        if (order == null)
        {
            throw new BusinessException("Order not found");
        }
        
        await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            order.PaymentMethod = paymentMethod;
            
            if (paymentMethod == "COD")
            {
                // For Cash on Delivery, set status to pending_payment and create delivery schedule
                order.Status = "pending_payment";
                
                // Create delivery schedule immediately for COD orders
                var deliveryDto = new DeliveryScheduleDto
                {
                    OrderId = orderId,
                    DeliveryTime = DateTime.UtcNow.AddDays(1),
                    Address = "Customer address", // Should come from customer profile
                    DriverContact = "TBD"
                };
                
                await _deliveryService.CreateDeliveryScheduleAsync(orderId, deliveryDto);
            }
            else if (paymentMethod == "VNPAY")
            {
                // For VNPAY, the payment URL will be generated separately
                // Status remains "pending" until payment callback is received
                order.Status = "pending";
            }
            else
            {
                throw new BusinessException($"Unsupported payment method: {paymentMethod}");
            }
            
            await _unitOfWork.Orders.UpdateAsync(order);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
            
            return MapToDto(order);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }
    
    public async Task<OrderDto> ProcessVnpayCallbackAsync(VnpayCallbackDto callbackDto)
    {
        var callbackResult = await _vnpayService.ProcessCallbackAsync(callbackDto);
        
        if (!callbackResult.IsSuccess)
        {
            throw new BusinessException($"Invalid VNPAY callback: {callbackResult.Message}");
        }
        
        var order = await _unitOfWork.Orders.GetByIdAsync(callbackResult.OrderId);
        
        if (order == null)
        {
            throw new BusinessException("Order not found");
        }
        
        await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            if (callbackResult.ResponseCode == "00") // Success
            {
                order.Status = "confirmed";
                order.VnpayTransactionId = callbackResult.TransactionId;
                order.PaymentConfirmedAt = DateTime.UtcNow;
                
                // Create delivery schedule for successful payment
                var deliveryDto = new DeliveryScheduleDto
                {
                    OrderId = order.Id,
                    DeliveryTime = DateTime.UtcNow.AddDays(1),
                    Address = "Customer address", // Should come from customer profile
                    DriverContact = "TBD"
                };
                
                await _deliveryService.CreateDeliveryScheduleAsync(order.Id, deliveryDto);
            }
            else
            {
                order.Status = "payment_failed";
                
                // Restore menu meal quantities
                foreach (var detail in order.OrderDetails)
                {
                    var menuMeal = await _unitOfWork.MenuMeals.GetByIdAsync(detail.MenuMealId);
                    menuMeal.AvailableQuantity += detail.Quantity;
                    await _unitOfWork.MenuMeals.UpdateAsync(menuMeal);
                }
            }
            
            await _unitOfWork.Orders.UpdateAsync(order);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
            
            return MapToDto(order);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }
    
    public async Task<OrderDto> ConfirmCashPaymentAsync(Guid orderId, Guid deliveryManId)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
        
        if (order == null)
        {
            throw new BusinessException("Order not found");
        }
        
        if (order.PaymentMethod != "COD")
        {
            throw new BusinessException("Order is not a Cash on Delivery order");
        }
        
        if (order.Status != "pending_payment")
        {
            throw new BusinessException($"Cannot confirm payment for order with status: {order.Status}");
        }
        
        await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            order.Status = "confirmed";
            order.PaymentConfirmedAt = DateTime.UtcNow;
            order.PaymentConfirmedBy = deliveryManId;
            
            await _unitOfWork.Orders.UpdateAsync(order);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
            
            return MapToDto(order);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }
}
}

// Nutrition Calculator Service
public class NutritionCalculatorService : INutritionCalculatorService
{
    private readonly IUnitOfWork _unitOfWork;
    
    public async Task<NutritionInfo> CalculateRecipeNutritionAsync(Guid recipeId)
    {
        var recipe = await _unitOfWork.Recipes.GetByIdAsync(recipeId);
        
        if (recipe == null)
        {
            throw new BusinessException("Recipe not found");
        }
        
        float totalCalories = 0;
        float totalProtein = 0;
        float totalFat = 0;
        float totalCarbs = 0;
        
        foreach (var recipeIngredient in recipe.RecipeIngredients)
        {
            var ingredient = recipeIngredient.Ingredient;
            var amount = recipeIngredient.Amount;
            
            // Calculate based on ingredient's calories per unit
            totalCalories += ingredient.CaloPerUnit * amount;
            
            // For simplicity, assume proportional macros
            // In real implementation, ingredients would have detailed macro info
            totalProtein += (ingredient.CaloPerUnit * amount) * 0.25f;
            totalFat += (ingredient.CaloPerUnit * amount) * 0.30f;
            totalCarbs += (ingredient.CaloPerUnit * amount) * 0.45f;
        }
        
        return new NutritionInfo
        {
            TotalCalories = totalCalories,
            ProteinG = totalProtein / 4, // 4 calories per gram of protein
            FatG = totalFat / 9, // 9 calories per gram of fat
            CarbsG = totalCarbs / 4 // 4 calories per gram of carbs
        };
    }
    
    public async Task UpdateRecipeNutritionAsync(Guid recipeId)
    {
        var nutrition = await CalculateRecipeNutritionAsync(recipeId);
        var recipe = await _unitOfWork.Recipes.GetByIdAsync(recipeId);
        
        recipe.TotalCalories = nutrition.TotalCalories;
        recipe.ProteinG = nutrition.ProteinG;
        recipe.FatG = nutrition.FatG;
        recipe.CarbsG = nutrition.CarbsG;
        
        await _unitOfWork.Recipes.UpdateAsync(recipe);
        await _unitOfWork.SaveChangesAsync();
    }
}

// VNPAY Service Implementation
public class VnpayService : IVnpayService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<VnpayService> _logger;
    
    private string VnpayUrl => _configuration["VnPay:Url"];
    private string VnpayTmnCode => _configuration["VnPay:TmnCode"];
    private string VnpayHashSecret => _configuration["VnPay:HashSecret"];
    private string VnpayReturnUrl => _configuration["VnPay:ReturnUrl"];
    
    public VnpayService(IConfiguration configuration, ILogger<VnpayService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }
    
    public async Task<VnpayPaymentUrlDto> CreatePaymentUrlAsync(Guid orderId, decimal amount, string orderInfo)
    {
        var vnpayData = new SortedDictionary<string, string>
        {
            {"vnp_Version", "2.1.0"},
            {"vnp_Command", "pay"},
            {"vnp_TmnCode", VnpayTmnCode},
            {"vnp_Amount", ((long)(amount * 100)).ToString()}, // VNPay expects amount in VND cents
            {"vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss")},
            {"vnp_CurrCode", "VND"},
            {"vnp_IpAddr", "127.0.0.1"}, // Should be actual client IP
            {"vnp_Locale", "vn"},
            {"vnp_OrderInfo", orderInfo},
            {"vnp_OrderType", "other"},
            {"vnp_ReturnUrl", VnpayReturnUrl},
            {"vnp_TxnRef", orderId.ToString()}
        };
        
        // Create secure hash
        var hashData = string.Join("&", vnpayData.Select(kv => $"{kv.Key}={kv.Value}"));
        var secureHash = CreateSecureHash(hashData, VnpayHashSecret);
        vnpayData.Add("vnp_SecureHash", secureHash);
        
        // Build payment URL
        var queryString = string.Join("&", vnpayData.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
        var paymentUrl = $"{VnpayUrl}?{queryString}";
        
        return new VnpayPaymentUrlDto
        {
            PaymentUrl = paymentUrl,
            TransactionId = orderId.ToString()
        };
    }
    
    public async Task<VnpayCallbackResult> ProcessCallbackAsync(VnpayCallbackDto callbackDto)
    {
        try
        {
            // Validate callback
            if (!ValidateCallback(callbackDto))
            {
                return new VnpayCallbackResult
                {
                    IsSuccess = false,
                    Message = "Invalid callback signature"
                };
            }
            
            // Parse order ID
            if (!Guid.TryParse(callbackDto.vnp_TxnRef, out var orderId))
            {
                return new VnpayCallbackResult
                {
                    IsSuccess = false,
                    Message = "Invalid order ID format"
                };
            }
            
            return new VnpayCallbackResult
            {
                IsSuccess = true,
                OrderId = orderId,
                TransactionId = callbackDto.vnp_TransactionNo,
                ResponseCode = callbackDto.vnp_ResponseCode,
                Message = GetResponseMessage(callbackDto.vnp_ResponseCode)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing VNPAY callback");
            return new VnpayCallbackResult
            {
                IsSuccess = false,
                Message = "Error processing callback"
            };
        }
    }
    
    public bool ValidateCallback(VnpayCallbackDto callbackDto)
    {
        try
        {
            // Extract all parameters except secure hash
            var vnpayData = new SortedDictionary<string, string>();
            
            var properties = typeof(VnpayCallbackDto).GetProperties();
            foreach (var prop in properties)
            {
                if (prop.Name == "vnp_SecureHash" || prop.Name == "vnp_SecureHashType")
                    continue;
                    
                var value = prop.GetValue(callbackDto)?.ToString();
                if (!string.IsNullOrEmpty(value))
                {
                    vnpayData.Add(prop.Name, value);
                }
            }
            
            // Create hash data
            var hashData = string.Join("&", vnpayData.Select(kv => $"{kv.Key}={kv.Value}"));
            var computedHash = CreateSecureHash(hashData, VnpayHashSecret);
            
            return computedHash.Equals(callbackDto.vnp_SecureHash, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating VNPAY callback");
            return false;
        }
    }
    
    private string CreateSecureHash(string data, string secretKey)
    {
        using (var hmac = new System.Security.Cryptography.HMACSHA512(Encoding.UTF8.GetBytes(secretKey)))
        {
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }
    
    private string GetResponseMessage(string responseCode)
    {
        return responseCode switch
        {
            "00" => "Payment successful",
            "07" => "Transaction deducted successfully. Transaction is suspected of fraud (related to gray card/black card)",
            "09" => "Customer's card/account has not registered for InternetBanking service at the bank",
            "10" => "Customer entered incorrect card/account information more than 3 times",
            "11" => "Payment deadline has expired. Please retry the transaction",
            "12" => "Customer's card/account is locked",
            "13" => "Customer entered incorrect transaction authentication password (OTP)",
            "24" => "Customer canceled the transaction",
            "51" => "Customer's account has insufficient balance to make the transaction",
            "65" => "Customer's account has exceeded the daily transaction limit",
            _ => "Transaction failed"
        };
    }
}
```


### Presentation Layer Components

#### Controllers

```csharp
// Account Controller
[Route("account")]
public class AccountController : Controller
{
    private readonly IAccountService _accountService;
    private readonly ILogger<AccountController> _logger;
    
    public AccountController(IAccountService accountService, ILogger<AccountController> logger)
    {
        _accountService = accountService;
        _logger = logger;
    }
    
    [HttpGet("register")]
    public IActionResult Register()
    {
        return View();
    }
    
    [HttpPost("register")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
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
            
            var account = await _accountService.RegisterAsync(dto);
            
            // Set authentication cookie
            await SignInUser(account);
            
            return RedirectToAction("Index", "Home");
        }
        catch (BusinessException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(model);
        }
    }
    
    [HttpGet("login")]
    public IActionResult Login()
    {
        return View();
    }
    
    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }
        
        try
        {
            var account = await _accountService.AuthenticateAsync(model.Email, model.Password);
            await SignInUser(account);
            
            return RedirectToAction("Index", "Home");
        }
        catch (AuthenticationException)
        {
            ModelState.AddModelError("", "Invalid email or password");
            return View(model);
        }
    }
    
    private async Task SignInUser(AccountDto account)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new Claim(ClaimTypes.Email, account.Email),
            new Claim(ClaimTypes.Name, account.FullName),
            new Claim(ClaimTypes.Role, account.Role)
        };
        
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    }
}

// Order Controller with enhanced payment processing
[Route("orders")]
[Authorize(Roles = "Customer")]
public class OrderController : Controller
{
    private readonly IOrderService _orderService;
    private readonly IMenuService _menuService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IVnpayService _vnpayService;
    
    public OrderController(
        IOrderService orderService, 
        IMenuService menuService, 
        ISubscriptionService subscriptionService,
        IVnpayService vnpayService)
    {
        _orderService = orderService;
        _menuService = menuService;
        _subscriptionService = subscriptionService;
        _vnpayService = vnpayService;
    }
    
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var accountId = GetCurrentAccountId();
        var orders = await _orderService.GetByAccountIdAsync(accountId);
        
        var viewModel = new OrderListViewModel
        {
            Orders = orders.Select(MapToViewModel).ToList()
        };
        
        return View(viewModel);
    }
    
    [HttpGet("create")]
    public async Task<IActionResult> Create()
    {
        // Check active subscription
        var accountId = GetCurrentAccountId();
        var hasSubscription = await _subscriptionService.HasActiveSubscriptionAsync(accountId);
        
        if (!hasSubscription)
        {
            return RedirectToAction("Index", "Subscription");
        }
        
        // Get today's menu
        var menu = await _menuService.GetByDateAsync(DateTime.Today);
        
        var viewModel = new CreateOrderViewModel
        {
            MenuMeals = menu.MenuMeals.Select(MapToViewModel).ToList()
        };
        
        return View(viewModel);
    }
    
    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateOrderViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }
        
        try
        {
            var accountId = GetCurrentAccountId();
            var items = model.SelectedItems.Select(i => new OrderItemDto
            {
                MenuMealId = i.MenuMealId,
                Quantity = i.Quantity
            }).ToList();
            
            var order = await _orderService.CreateOrderAsync(accountId, items);
            
            return RedirectToAction("Payment", new { orderId = order.Id });
        }
        catch (BusinessException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(model);
        }
    }
    
    [HttpGet("payment/{orderId}")]
    public async Task<IActionResult> Payment(Guid orderId)
    {
        var order = await _orderService.GetByIdAsync(orderId);
        
        if (order == null || order.AccountId != GetCurrentAccountId())
        {
            return NotFound();
        }
        
        var viewModel = new PaymentViewModel
        {
            OrderId = order.Id,
            TotalAmount = order.TotalAmount,
            PaymentMethods = new List<PaymentMethodOption>
            {
                new PaymentMethodOption { Value = "COD", Text = "Cash on Delivery (COD)" },
                new PaymentMethodOption { Value = "VNPAY", Text = "Online Payment (VNPAY)" }
            }
        };
        
        return View(viewModel);
    }
    
    [HttpPost("payment")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessPayment(PaymentViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("Payment", model);
        }
        
        try
        {
            if (model.PaymentMethod == "VNPAY")
            {
                // For VNPAY, redirect to payment gateway
                var order = await _orderService.ProcessPaymentAsync(model.OrderId, model.PaymentMethod);
                var paymentUrl = await _vnpayService.CreatePaymentUrlAsync(
                    model.OrderId, 
                    model.TotalAmount, 
                    $"Payment for Order {model.OrderId}");
                
                return Redirect(paymentUrl.PaymentUrl);
            }
            else if (model.PaymentMethod == "COD")
            {
                // For COD, process immediately and show confirmation
                var order = await _orderService.ProcessPaymentAsync(model.OrderId, model.PaymentMethod);
                return RedirectToAction("Confirmation", new { orderId = order.Id });
            }
            else
            {
                ModelState.AddModelError("", "Invalid payment method selected.");
                return View("Payment", model);
            }
        }
        catch (BusinessException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View("Payment", model);
        }
    }
    
    [HttpGet("vnpay-callback")]
    [AllowAnonymous]
    public async Task<IActionResult> VnpayCallback([FromQuery] VnpayCallbackDto callbackDto)
    {
        try
        {
            var order = await _orderService.ProcessVnpayCallbackAsync(callbackDto);
            
            if (order.Status == "confirmed")
            {
                return RedirectToAction("Confirmation", new { orderId = order.Id });
            }
            else
            {
                return RedirectToAction("PaymentFailed", new { orderId = order.Id });
            }
        }
        catch (BusinessException ex)
        {
            return RedirectToAction("PaymentError", new { message = ex.Message });
        }
    }
    
    [HttpGet("confirmation/{orderId}")]
    public async Task<IActionResult> Confirmation(Guid orderId)
    {
        var order = await _orderService.GetByIdAsync(orderId);
        
        if (order == null || order.AccountId != GetCurrentAccountId())
        {
            return NotFound();
        }
        
        return View(order);
    }
    
    [HttpGet("payment-failed/{orderId}")]
    public async Task<IActionResult> PaymentFailed(Guid orderId)
    {
        var order = await _orderService.GetByIdAsync(orderId);
        
        if (order == null || order.AccountId != GetCurrentAccountId())
        {
            return NotFound();
        }
        
        return View(order);
    }
    
    [HttpGet("payment-error")]
    public IActionResult PaymentError(string message)
    {
        ViewBag.ErrorMessage = message;
        return View();
    }
    
    private Guid GetCurrentAccountId()
    {
        var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return Guid.Parse(accountIdClaim.Value);
    }
}

// Delivery Controller for delivery men to confirm cash payments
[Route("delivery")]
[Authorize(Roles = "DeliveryMan")]
public class DeliveryController : Controller
{
    private readonly IOrderService _orderService;
    private readonly IDeliveryService _deliveryService;
    
    public DeliveryController(IOrderService orderService, IDeliveryService deliveryService)
    {
        _orderService = orderService;
        _deliveryService = deliveryService;
    }
    
    [HttpGet("assigned")]
    public async Task<IActionResult> AssignedDeliveries()
    {
        var deliveryManId = GetCurrentAccountId();
        var deliveries = await _deliveryService.GetByDeliveryManAsync(deliveryManId);
        
        var viewModel = new AssignedDeliveriesViewModel
        {
            Deliveries = deliveries.Select(MapToViewModel).ToList()
        };
        
        return View(viewModel);
    }
    
    [HttpPost("confirm-cash-payment")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmCashPayment(Guid orderId)
    {
        try
        {
            var deliveryManId = GetCurrentAccountId();
            var order = await _orderService.ConfirmCashPaymentAsync(orderId, deliveryManId);
            
            TempData["SuccessMessage"] = "Cash payment confirmed successfully.";
            return RedirectToAction("AssignedDeliveries");
        }
        catch (BusinessException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction("AssignedDeliveries");
        }
    }
    
    [HttpPost("complete-delivery")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteDelivery(Guid deliveryId)
    {
        try
        {
            await _deliveryService.CompleteDeliveryAsync(deliveryId);
            
            TempData["SuccessMessage"] = "Delivery completed successfully.";
            return RedirectToAction("AssignedDeliveries");
        }
        catch (BusinessException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction("AssignedDeliveries");
        }
    }
    
    private Guid GetCurrentAccountId()
    {
        var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return Guid.Parse(accountIdClaim.Value);
    }
}

// Admin Controller
[Route("admin")]
[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly IRevenueService _revenueService;
    private readonly ISubscriptionService _subscriptionService;
    
    public AdminController(IRevenueService revenueService, ISubscriptionService subscriptionService)
    {
        _revenueService = revenueService;
        _subscriptionService = subscriptionService;
    }
    
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var stats = await _revenueService.GetDashboardStatsAsync();
        
        var viewModel = new AdminDashboardViewModel
        {
            TotalCustomers = stats.TotalCustomers,
            ActiveSubscriptions = stats.ActiveSubscriptions,
            PendingOrders = stats.PendingOrders,
            CurrentMonthRevenue = stats.CurrentMonthRevenue
        };
        
        return View(viewModel);
    }
    
    [HttpGet("revenue")]
    public async Task<IActionResult> Revenue(int? year, int? month)
    {
        var currentYear = year ?? DateTime.Now.Year;
        var currentMonth = month ?? DateTime.Now.Month;
        
        var report = await _revenueService.GetMonthlyReportAsync(currentYear, currentMonth);
        
        var viewModel = new RevenueReportViewModel
        {
            Year = currentYear,
            Month = currentMonth,
            TotalSubscriptionRevenue = report.TotalSubscriptionRevenue,
            TotalOrderRevenue = report.TotalOrderRevenue,
            TotalOrdersCount = report.TotalOrdersCount
        };
        
        return View(viewModel);
    }
}
```


#### ViewModels

```csharp
// Account ViewModels
public class RegisterViewModel
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; }
    
    [Required(ErrorMessage = "Password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    public string Password { get; set; }
    
    [Required(ErrorMessage = "Full name is required")]
    public string FullName { get; set; }
}

public class LoginViewModel
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; }
    
    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; }
}

// Order ViewModels
public class CreateOrderViewModel
{
    public List<MenuMealViewModel> MenuMeals { get; set; }
    public List<OrderItemViewModel> SelectedItems { get; set; }
}

public class MenuMealViewModel
{
    public Guid MenuMealId { get; set; }
    public string RecipeName { get; set; }
    public decimal Price { get; set; }
    public int AvailableQuantity { get; set; }
}

public class OrderItemViewModel
{
    public Guid MenuMealId { get; set; }
    
    [Range(1, 100, ErrorMessage = "Quantity must be between 1 and 100")]
    public int Quantity { get; set; }
}

public class PaymentViewModel
{
    public Guid OrderId { get; set; }
    public decimal TotalAmount { get; set; }
    
    [Required(ErrorMessage = "Payment method is required")]
    public string PaymentMethod { get; set; }
    
    public List<PaymentMethodOption> PaymentMethods { get; set; } = new List<PaymentMethodOption>();
}

public class PaymentMethodOption
{
    public string Value { get; set; }
    public string Text { get; set; }
}

public class AssignedDeliveriesViewModel
{
    public List<DeliveryScheduleViewModel> Deliveries { get; set; } = new List<DeliveryScheduleViewModel>();
}

public class DeliveryScheduleViewModel
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public DateTime DeliveryTime { get; set; }
    public string Address { get; set; }
    public string DriverContact { get; set; }
    public string OrderStatus { get; set; }
    public string PaymentMethod { get; set; }
    public decimal TotalAmount { get; set; }
    public bool CanConfirmPayment => PaymentMethod == "COD" && OrderStatus == "pending_payment";
    public bool CanCompleteDelivery => OrderStatus == "confirmed";
}

// Health Profile ViewModels
public class HealthProfileViewModel
{
    [Range(1, 150, ErrorMessage = "Age must be between 1 and 150")]
    public int Age { get; set; }
    
    [Range(0.1, 500, ErrorMessage = "Weight must be positive")]
    public float Weight { get; set; }
    
    [Range(0.1, 300, ErrorMessage = "Height must be positive")]
    public float Height { get; set; }
    
    [Required(ErrorMessage = "Gender is required")]
    public string Gender { get; set; }
    
    public string HealthNotes { get; set; }
    
    public List<Guid> SelectedAllergies { get; set; }
    public List<Guid> SelectedFoodPreferences { get; set; }
}
```

#### Authorization Filters

```csharp
// Custom authorization filter for subscription requirement
public class RequireActiveSubscriptionAttribute : TypeFilterAttribute
{
    public RequireActiveSubscriptionAttribute() : base(typeof(RequireActiveSubscriptionFilter))
    {
    }
}

public class RequireActiveSubscriptionFilter : IAsyncActionFilter
{
    private readonly ISubscriptionService _subscriptionService;
    
    public RequireActiveSubscriptionFilter(ISubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }
    
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var accountIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
        
        if (accountIdClaim == null)
        {
            context.Result = new UnauthorizedResult();
            return;
        }
        
        var accountId = Guid.Parse(accountIdClaim.Value);
        var hasActiveSubscription = await _subscriptionService.HasActiveSubscriptionAsync(accountId);
        
        if (!hasActiveSubscription)
        {
            context.Result = new RedirectToActionResult("Index", "Subscription", null);
            return;
        }
        
        await next();
    }
}
```

## Data Models

### Database Schema

The database schema follows the ERD diagram provided in `diagrams/erd.txt`. Key tables include:

**Core Tables**:
- `ACCOUNT`: User accounts with authentication
- `HEALTH_PROFILE`: Customer health information
- `ALLERGY`: Allergen definitions
- `FOOD_PREFERENCE`: Food preference definitions
- `SUBSCRIPTION_PACKAGE`: Available subscription plans
- `USER_SUBSCRIPTION`: Customer subscriptions

**Meal Planning Tables**:
- `MEAL_PLAN`: Meal plans (AI or manual)
- `MEAL`: Individual meals within plans
- `RECIPE`: Recipe definitions with nutrition
- `INGREDIENT`: Ingredient definitions
- `RECIPE_INGREDIENT`: Recipe-ingredient relationships

**Ordering Tables**:
- `DAILY_MENU`: Daily menu definitions
- `MENU_MEAL`: Meals available in menus
- `ORDER`: Customer orders
- `ORDER_DETAIL`: Order line items
- `DELIVERY_SCHEDULE`: Delivery information

**Management Tables**:
- `FRIDGE_ITEM`: Virtual fridge inventory
- `REVENUE_REPORT`: Financial reports

### Key Relationships

1. **One-to-One**: Account ↔ HealthProfile, Order ↔ DeliverySchedule
2. **One-to-Many**: Account → Orders, MealPlan → Meals, Order → OrderDetails
3. **Many-to-Many**: HealthProfile ↔ Allergies, HealthProfile ↔ FoodPreferences, Recipe ↔ Ingredients

### Indexes and Constraints

- Unique index on `Account.Email`
- Composite index on `UserSubscription(AccountId, Status, EndDate)` for active subscription queries
- Index on `DailyMenu.MenuDate` for date-based queries
- Index on `Order.OrderDate` for reporting
- Check constraints for positive values (weight, height, quantities, prices)
- Foreign key constraints with cascade delete where appropriate


## Correctness Properties

A property is a characteristic or behavior that should hold true across all valid executions of a system—essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.

### Property Reflection

After analyzing all acceptance criteria, I identified several areas of redundancy that need consolidation:

**Redundancy Analysis**:
1. Properties 2.2, 2.3, 2.4, 2.5 (allergy/preference management) can be combined into two properties about link management
2. Properties 3.3 and 5.2 (AI-generated flag) can be combined into one property about flag correctness
3. Properties 7.6 and 7.7 (expiry date checking) can be combined into one property about expiry status
4. Properties 8.3 and 8.5 (menu status) can be combined into one property about status transitions
5. Properties 12.4 and 13.1 are duplicates - consolidate into one dashboard property
6. Properties 14.1-14.5 (role-based access) can be combined into fewer comprehensive properties
7. Properties 15.3, 15.4, 15.5, 15.7 (validation) can be combined into comprehensive validation properties

### Account Management Properties

Property 1: Account creation sets customer role
*For any* valid email, password, and full name, creating a new account should result in an account with role "Customer"
**Validates: Requirements 1.1**

Property 2: Duplicate email rejection
*For any* email that already exists in the system, attempting to register with that email should be rejected with an error
**Validates: Requirements 1.2**

Property 3: Authentication with valid credentials
*For any* account with valid credentials, authentication should succeed and create a session
**Validates: Requirements 1.4**

Property 4: Authentication with invalid credentials
*For any* account, authentication with an incorrect password should be rejected with an error
**Validates: Requirements 1.5**

Property 5: Password hashing
*For any* password provided during registration, the stored password hash should not equal the plain text password
**Validates: Requirements 1.6**

Property 6: Role-based authorization
*For any* user with an active session, access to features should be granted or denied based on their role
**Validates: Requirements 1.7, 14.1, 14.2, 14.3, 14.4, 14.5, 14.6**


### Health Profile Properties

Property 7: Health profile storage
*For any* valid age, weight, height, gender, and optional health notes, creating a health profile should store all provided fields
**Validates: Requirements 2.1**

Property 8: Allergy and preference link management
*For any* health profile and allergy/preference, adding the link should make it retrievable, and removing it should make it no longer retrievable
**Validates: Requirements 2.2, 2.3, 2.4, 2.5**

Property 9: Positive weight and height validation
*For any* weight or height value that is zero or negative, updating a health profile should reject the input with an error
**Validates: Requirements 2.6**

Property 10: Age range validation
*For any* age value outside the range [1, 150], updating a health profile should reject the input with an error
**Validates: Requirements 2.7**

### Subscription Properties

Property 11: Subscription package display
*For any* set of subscription packages, viewing packages should return all packages with name, price, duration, and description
**Validates: Requirements 3.1**

Property 12: Subscription date calculation
*For any* subscription package with duration_days, creating a subscription should set end_date to start_date plus duration_days
**Validates: Requirements 3.2**

Property 13: Subscription status lifecycle
*For any* new subscription, the initial status should be "active", and when end_date is reached, the status should become "expired"
**Validates: Requirements 3.3, 3.4**

Property 14: Subscription requirement enforcement
*For any* customer without an active subscription, attempting to access subscription-required features should be denied
**Validates: Requirements 3.5, 4.7, 9.8**

### Meal Plan Generation Properties

Property 15: Meal plan date range coverage
*For any* date range specified for meal plan generation, the system should create meals for each day with meal types (breakfast, lunch, dinner)
**Validates: Requirements 4.2**

Property 16: Allergen exclusion
*For any* customer with allergies, AI-generated meal plans should not contain recipes with ingredients matching those allergies
**Validates: Requirements 4.3**

Property 17: Food preference prioritization
*For any* customer with food preferences, AI-generated meal plans should prioritize recipes matching those preferences
**Validates: Requirements 4.4**

Property 18: AI generation flag correctness
*For any* meal plan, if it was AI-generated then is_ai_generated should be true, and if it was manually created then is_ai_generated should be false
**Validates: Requirements 4.5, 5.2**

Property 19: Meal plan account association
*For any* meal plan created by a customer, the plan should be associated with that customer's account
**Validates: Requirements 4.6**

Property 20: Manual meal plan creation
*For any* valid plan name, start date, and end date, creating a manual meal plan should accept all fields
**Validates: Requirements 5.1**

Property 21: Meal date range validation
*For any* meal added to a plan, if the serve_date is outside the plan's date range, the addition should be rejected
**Validates: Requirements 5.3**

Property 22: Recipe-meal linking
*For any* recipes added to a meal, all selected recipes should be linked to that meal
**Validates: Requirements 5.4**

Property 23: Personal recipe creation
*For any* recipe with specified ingredients and portions, creating the recipe should store all ingredient links with amounts
**Validates: Requirements 5.6**


### Nutrition Calculation Properties

Property 24: Recipe nutrition calculation
*For any* recipe with ingredients, the calculated nutrition (calories, protein, fat, carbs) should equal the sum of (ingredient.calo_per_unit × recipe_ingredient.amount) for all ingredients
**Validates: Requirements 6.1, 6.2**

Property 25: Meal nutrition aggregation
*For any* meal with multiple recipes, the displayed nutrition should equal the sum of nutritional values from all recipes in that meal
**Validates: Requirements 6.3**

Property 26: Daily nutrition aggregation
*For any* meal plan and date, the daily nutrition should equal the sum of all meals on that date, and total nutrition should equal the sum across all days
**Validates: Requirements 6.4**

Property 27: Nutrition recalculation on recipe update
*For any* recipe that is updated, the nutrition values should be recalculated based on the new ingredients
**Validates: Requirements 11.5**

### Virtual Fridge Properties

Property 28: Fridge item storage
*For any* ingredient, current_amount, and expiry_date, adding a fridge item should store all three fields
**Validates: Requirements 7.1**

Property 29: Non-negative quantity validation
*For any* fridge item, updating the quantity to a negative value should be rejected with an error
**Validates: Requirements 7.2**

Property 30: Fridge item deletion
*For any* fridge item, removing it should make it no longer retrievable from the fridge
**Validates: Requirements 7.3**

Property 31: Fridge item retrieval
*For any* customer's fridge items, viewing the fridge should return all items with quantities and expiry dates
**Validates: Requirements 7.4**

Property 32: Grocery list generation
*For any* meal plan and fridge inventory, the grocery list should contain ingredients that are either missing from the fridge or have insufficient quantity
**Validates: Requirements 7.5**

Property 33: Expiry status determination
*For any* fridge item, if expiry_date is within 3 days it should be marked as expiring soon, and if expiry_date has passed it should be marked as expired
**Validates: Requirements 7.6, 7.7**

### Menu Management Properties

Property 34: Menu creation with draft status
*For any* menu_date, creating a daily menu should set the initial status to "draft"
**Validates: Requirements 8.1**

Property 35: Menu meal required fields
*For any* menu meal, adding it to a menu should require recipe, price, and available_quantity
**Validates: Requirements 8.2**

Property 36: Menu status transitions
*For any* menu, publishing it should change status from "draft" to "active", and only active menus should be visible to guests and customers
**Validates: Requirements 8.3, 8.5**

Property 37: Menu quantity validation
*For any* menu meal, updating available_quantity to a negative value should be rejected with an error
**Validates: Requirements 8.4**

Property 38: Weekly menu date range
*For any* start date, viewing the weekly menu should return all active menus for the next 7 days from that date
**Validates: Requirements 8.6**

Property 39: Sold out status
*For any* menu meal, when available_quantity reaches zero, it should be marked as sold out
**Validates: Requirements 8.7**


### Order Processing Properties

Property 40: Order detail creation
*For any* menu meals added to an order, order details should be created with quantity and unit_price from the menu meal
**Validates: Requirements 9.1**

Property 41: Order quantity validation
*For any* order, if any menu meal has insufficient available_quantity, the order creation should be rejected with an error
**Validates: Requirements 9.2**

Property 42: Order total calculation
*For any* order with order details, total_amount should equal the sum of (quantity × unit_price) for all order details
**Validates: Requirements 9.3**

Property 43: Inventory reduction on order
*For any* order that is placed, each menu meal's available_quantity should be reduced by the ordered quantity
**Validates: Requirements 9.4**

Property 44: Payment failure rollback
*For any* order where payment fails, the order status should be "payment_failed" and all menu meal quantities should be restored to their original values
**Validates: Requirements 9.6**

Property 45: Payment success workflow
*For any* order where payment succeeds (VNPAY) or cash is confirmed (COD), the order status should be "confirmed" and a delivery schedule should be created
**Validates: Requirements 9.7, 9.11**

Property 69: COD order status workflow
*For any* order with payment method "COD", the initial status should be "pending_payment" and a delivery schedule should be created immediately
**Validates: Requirements 9.5**

Property 70: VNPAY payment callback processing
*For any* VNPAY payment callback with valid signature, the system should update order status based on the response code and create delivery schedule for successful payments
**Validates: Requirements 9.6, 9.7**

Property 71: Cash payment confirmation authorization
*For any* COD order, only delivery men should be able to confirm cash payment, and only when order status is "pending_payment"
**Validates: Requirements 9.10, 10.6, 10.7**

Property 72: Payment method validation
*For any* order payment processing, only supported payment methods ("COD", "VNPAY") should be accepted
**Validates: Requirements 9.5, 9.6**

Property 73: VNPAY callback validation
*For any* VNPAY callback, the secure hash should be validated before processing the payment result
**Validates: Requirements 9.6, 9.7**

### Delivery Management Properties

Property 46: Delivery schedule creation
*For any* confirmed order, a delivery schedule should be created with delivery_time, address, and driver_contact
**Validates: Requirements 10.1**

Property 47: Customer delivery retrieval
*For any* customer, viewing delivery schedules should return all deliveries for that customer's orders
**Validates: Requirements 10.2**

Property 48: Delivery man assignment filtering
*For any* delivery man, viewing delivery schedules should return only deliveries assigned to that delivery man
**Validates: Requirements 10.3**

Property 49: Delivery completion status update
*For any* delivery that is completed, the associated order status should be updated to "delivered"
**Validates: Requirements 10.4**

Property 50: Future delivery time validation
*For any* delivery time update, if the new delivery_time is in the past, the update should be rejected with an error
**Validates: Requirements 10.5**

### Recipe and Ingredient Properties

Property 51: Recipe required fields
*For any* recipe creation, recipe_name and instructions should be required fields
**Validates: Requirements 11.1**

Property 52: Recipe ingredient required fields
*For any* ingredient added to a recipe, the ingredient and amount should be required
**Validates: Requirements 11.2**

Property 53: Ingredient required fields
*For any* ingredient creation, ingredient_name, unit, and calo_per_unit should be required fields
**Validates: Requirements 11.3**

Property 54: Allergen flag storage
*For any* ingredient created with is_allergen flag, the flag value should be stored and retrievable
**Validates: Requirements 11.4**

Property 55: Recipe deletion constraint
*For any* recipe used in active menu meals, attempting to delete the recipe should be prevented with an error
**Validates: Requirements 11.6**


### Revenue Reporting Properties

Property 56: Monthly subscription revenue calculation
*For any* month and year, the monthly revenue report should calculate total_subscription_rev as the sum of all subscription package prices for subscriptions created in that month
**Validates: Requirements 12.1**

Property 57: Monthly order revenue calculation
*For any* month and year, the monthly revenue report should calculate total_order_rev as the sum of all order total_amounts for orders placed in that month
**Validates: Requirements 12.2**

Property 58: Monthly order count
*For any* month and year, the monthly revenue report should count total_orders_count as the number of orders placed in that month
**Validates: Requirements 12.3**

Property 59: Dashboard statistics aggregation
*For any* admin dashboard view, it should display current month revenue, order count, active subscription count, total customers, and pending orders calculated from current data
**Validates: Requirements 12.4, 13.1**

Property 60: Yearly revenue aggregation
*For any* year, the yearly revenue should equal the sum of monthly total revenues (subscription + order) for all 12 months
**Validates: Requirements 12.5**

Property 61: Revenue report persistence
*For any* generated revenue report, it should be stored with month, year, and all calculated values
**Validates: Requirements 12.6**

Property 62: AI operation logging
*For any* AI operation (meal plan generation, nutrition calculation, price adjustment), the operation should be logged for audit purposes
**Validates: Requirements 13.5**

### Data Validation Properties

Property 63: Descriptive error messages
*For any* invalid input to any component, the error message should be descriptive without exposing internal system details
**Validates: Requirements 15.1**

Property 64: Past date rejection for future operations
*For any* operation requiring a future date (delivery time, menu date, subscription end date), providing a past date should be rejected with an error
**Validates: Requirements 15.3**

Property 65: Negative value rejection
*For any* field requiring positive values (quantities, prices, weight, height), providing a negative value should be rejected with an error
**Validates: Requirements 15.4, 2.6, 7.2, 8.4**

Property 66: Required field validation
*For any* entity creation or update, omitting a required field should be rejected with an error specifying which fields are required
**Validates: Requirements 15.5**

Property 67: Foreign key constraint enforcement
*For any* operation that would violate a foreign key constraint, the operation should be prevented and return an error
**Validates: Requirements 15.6**

Property 68: Email format validation
*For any* email address, it should be validated against standard email format (RFC 5322) before being stored
**Validates: Requirements 15.7**


## Error Handling

### Exception Hierarchy

```csharp
// Base exception for business logic errors
public class BusinessException : Exception
{
    public BusinessException(string message) : base(message) { }
    public BusinessException(string message, Exception innerException) 
        : base(message, innerException) { }
}

// Authentication failures
public class AuthenticationException : BusinessException
{
    public AuthenticationException(string message) : base(message) { }
}

// Authorization failures
public class AuthorizationException : BusinessException
{
    public AuthorizationException(string message) : base(message) { }
}

// Validation failures
public class ValidationException : BusinessException
{
    public Dictionary<string, string[]> Errors { get; set; }
    
    public ValidationException(string message, Dictionary<string, string[]> errors) 
        : base(message)
    {
        Errors = errors;
    }
}

// Resource not found
public class NotFoundException : BusinessException
{
    public NotFoundException(string resourceType, Guid id) 
        : base($"{resourceType} with ID {id} not found") { }
}

// Constraint violations
public class ConstraintViolationException : BusinessException
{
    public ConstraintViolationException(string message) : base(message) { }
}
```

### Error Handling Strategy

**Service Layer**:
- Validate all inputs before processing
- Throw specific business exceptions for business rule violations
- Log all exceptions with context information
- Use transactions for multi-step operations with rollback on failure

**Controller Layer**:
- Catch business exceptions and convert to appropriate HTTP responses
- Return 400 Bad Request for validation errors
- Return 401 Unauthorized for authentication failures
- Return 403 Forbidden for authorization failures
- Return 404 Not Found for missing resources
- Return 500 Internal Server Error for unexpected errors
- Never expose internal error details to clients

**Global Exception Handler**:
```csharp
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    
    public async Task<bool> TryHandleAsync(
        HttpContext httpContext, 
        Exception exception, 
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "An error occurred: {Message}", exception.Message);
        
        var (statusCode, message) = exception switch
        {
            ValidationException ve => (400, ve.Message),
            AuthenticationException => (401, "Authentication failed"),
            AuthorizationException => (403, "Access denied"),
            NotFoundException => (404, "Resource not found"),
            ConstraintViolationException => (409, exception.Message),
            BusinessException => (400, exception.Message),
            _ => (500, "An unexpected error occurred")
        };
        
        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(new { error = message }, cancellationToken);
        
        return true;
    }
}
```

### Validation Strategy

**Input Validation**:
- Use FluentValidation for DTO validation
- Validate at service layer before processing
- Return detailed validation errors with field names

**Business Rule Validation**:
- Check business constraints (active subscription, sufficient quantity, etc.)
- Throw specific business exceptions with clear messages
- Validate state transitions (order status, menu status, etc.)

**Database Validation**:
- Use EF Core data annotations for basic constraints
- Configure check constraints in DbContext
- Handle constraint violations gracefully


## Testing Strategy

### Dual Testing Approach

The system will use both unit tests and property-based tests for comprehensive coverage:

**Unit Tests**:
- Test specific examples and edge cases
- Test integration points between components
- Test error conditions and exception handling
- Test controller actions and view rendering
- Test service layer business logic with mocked dependencies
- Test repository implementations with in-memory database

**Property-Based Tests**:
- Test universal properties across all inputs
- Use FsCheck library for C# property-based testing
- Generate random valid inputs to verify properties hold
- Minimum 100 iterations per property test
- Each property test references its design document property

### Property-Based Testing Configuration

**Library**: FsCheck for C#
- Install via NuGet: `FsCheck.Xunit`
- Integrate with xUnit test framework
- Configure generators for domain entities

**Test Configuration**:
```csharp
[Property(MaxTest = 100)]
public Property PropertyName(/* generated parameters */)
{
    // Feature: meal-prep-service, Property N: [property text]
    // Test implementation
}
```

**Custom Generators**:
```csharp
// Generator for valid emails
public static Arbitrary<string> ValidEmail()
{
    return Arb.From(
        from name in Arb.Generate<NonEmptyString>()
        from domain in Arb.Generate<NonEmptyString>()
        select $"{name.Get}@{domain.Get}.com"
    );
}

// Generator for valid health profiles
public static Arbitrary<HealthProfileDto> ValidHealthProfile()
{
    return Arb.From(
        from age in Gen.Choose(1, 150)
        from weight in Gen.Choose(1, 500).Select(w => (float)w)
        from height in Gen.Choose(1, 300).Select(h => (float)h)
        from gender in Gen.Elements("Male", "Female", "Other")
        select new HealthProfileDto
        {
            Age = age,
            Weight = weight,
            Height = height,
            Gender = gender
        }
    );
}

// Generator for valid date ranges
public static Arbitrary<(DateTime start, DateTime end)> ValidDateRange()
{
    return Arb.From(
        from days in Gen.Choose(1, 30)
        let start = DateTime.Today
        let end = start.AddDays(days)
        select (start, end)
    );
}
```

### Test Organization

**Project Structure**:
```
MealPrepService.Tests/
├── Unit/
│   ├── Services/
│   │   ├── AccountServiceTests.cs
│   │   ├── OrderServiceTests.cs
│   │   └── ...
│   ├── Controllers/
│   │   ├── AccountControllerTests.cs
│   │   └── ...
│   └── Repositories/
│       ├── AccountRepositoryTests.cs
│       └── ...
├── Properties/
│   ├── AccountPropertiesTests.cs
│   ├── OrderPropertiesTests.cs
│   ├── SubscriptionPropertiesTests.cs
│   └── ...
└── Integration/
    ├── OrderWorkflowTests.cs
    ├── MealPlanWorkflowTests.cs
    └── ...
```

### Property Test Examples

**Property 1: Account creation sets customer role**
```csharp
[Property(MaxTest = 100)]
public Property AccountCreationSetsCustomerRole(NonEmptyString email, NonEmptyString password, NonEmptyString fullName)
{
    // Feature: meal-prep-service, Property 1: Account creation sets customer role
    var validEmail = $"{email.Get}@example.com";
    var dto = new CreateAccountDto
    {
        Email = validEmail,
        Password = password.Get,
        FullName = fullName.Get
    };
    
    var account = _accountService.RegisterAsync(dto).Result;
    
    return (account.Role == "Customer").ToProperty();
}
```

**Property 42: Order total calculation**
```csharp
[Property(MaxTest = 100)]
public Property OrderTotalCalculation(PositiveInt itemCount)
{
    // Feature: meal-prep-service, Property 42: Order total calculation
    var items = GenerateRandomOrderItems(itemCount.Get);
    var order = _orderService.CreateOrderAsync(Guid.NewGuid(), items).Result;
    
    var expectedTotal = items.Sum(i => i.Quantity * i.UnitPrice);
    
    return (order.TotalAmount == expectedTotal).ToProperty();
}
```

**Property 16: Allergen exclusion**
```csharp
[Property(MaxTest = 100)]
public Property AllergenExclusionInMealPlans(Guid customerId, NonEmptyArray<Guid> allergyIds)
{
    // Feature: meal-prep-service, Property 16: Allergen exclusion
    // Setup customer with allergies
    SetupCustomerWithAllergies(customerId, allergyIds.Get);
    
    // Generate meal plan
    var mealPlan = _mealPlanService.GenerateAiMealPlanAsync(
        customerId, 
        DateTime.Today, 
        DateTime.Today.AddDays(7)
    ).Result;
    
    // Check no recipes contain allergens
    var hasAllergens = mealPlan.Meals
        .SelectMany(m => m.Recipes)
        .SelectMany(r => r.Ingredients)
        .Any(i => allergyIds.Get.Contains(i.AllergyId));
    
    return (!hasAllergens).ToProperty();
}
```

### Integration Testing

**Test Scenarios**:
1. Complete order workflow: Browse menu → Add to cart → Checkout → Payment → Delivery
2. Meal plan workflow: Create health profile → Subscribe → Generate meal plan → View nutrition
3. Fridge workflow: Add ingredients → Create meal plan → Generate grocery list
4. Admin workflow: View dashboard → Generate revenue report → Adjust prices

**Test Database**:
- Use SQLite in-memory database for tests
- Reset database between tests
- Seed test data as needed

### Test Coverage Goals

- **Unit Tests**: 80% code coverage minimum
- **Property Tests**: All 68 correctness properties implemented
- **Integration Tests**: All major workflows covered
- **Controller Tests**: All endpoints tested with valid and invalid inputs

### Continuous Integration

- Run all tests on every commit
- Fail build if any test fails
- Generate coverage reports
- Run property tests with increased iterations (1000+) in CI pipeline

