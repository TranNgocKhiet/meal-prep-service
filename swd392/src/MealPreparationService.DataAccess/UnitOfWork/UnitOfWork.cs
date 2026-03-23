using MealPreparationService.DataAccess.Data;
using MealPreparationService.DataAccess.Repositories;
using MealPreparationService.Domain.Services;
using Microsoft.EntityFrameworkCore.Storage;

namespace MealPreparationService.DataAccess.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly IDateTimeService _dateTimeService;
    private IDbContextTransaction? _transaction;
    
    private IAccountRepository? _accountRepository;
    private IRoleRepository? _roleRepository;
    private IGoogleAuthRepository? _googleAuthRepository;
    private IMealPlanRepository? _mealPlanRepository;
    private IRecipeRepository? _recipeRepository;
    private IIngredientRepository? _ingredientRepository;
    private IOrderRepository? _orderRepository;
    private INutrientRepository? _nutrientRepository;
    private IAllergyRepository? _allergyRepository;
    private IStatusRepository? _statusRepository;
    private IIngredientAllergyRepository? _ingredientAllergyRepository;
    private IHealthProfileRepository? _healthProfileRepository;
    private IHealthProfileAllergyRepository? _healthProfileAllergyRepository;
    private IHealthProfileIngredientRepository? _healthProfileIngredientRepository;
    private IFridgeRepository? _fridgeRepository;
    private IFridgeItemRepository? _fridgeItemRepository;
    private ICartRepository? _cartRepository;
    private ICartItemRepository? _cartItemRepository;
    private IMenuMealRepository? _menuMealRepository;
    private IDailyMenuRepository? _dailyMenuRepository;
    private IMenuMealRecipeRepository? _menuMealRecipeRepository;
    private IRecipeIngredientRepository? _recipeIngredientRepository;
    private IOrderDetailRepository? _orderDetailRepository;
    private IDeliveryScheduleRepository? _deliveryScheduleRepository;
    private IPaymentGatewayRepository? _paymentGatewayRepository;
    private ISubscriptionPackageRepository? _subscriptionPackageRepository;
    private IUserSubscriptionRepository? _userSubscriptionRepository;
    private IAICreditPackageRepository? _aiCreditPackageRepository;
    private IAICreditTransactionRepository? _aiCreditTransactionRepository;
    private IRevenueReportRepository? _revenueReportRepository;
    private ISystemConfigurationRepository? _systemConfigurationRepository;

    public UnitOfWork(ApplicationDbContext context, IDateTimeService dateTimeService)
    {
        _context = context;
        _dateTimeService = dateTimeService;
    }

    public IAccountRepository Accounts => _accountRepository ??= new AccountRepository(_context);
    public IRoleRepository Roles => _roleRepository ??= new RoleRepository(_context);
    public IGoogleAuthRepository GoogleAuths => _googleAuthRepository ??= new GoogleAuthRepository(_context);
    public IMealPlanRepository MealPlans => _mealPlanRepository ??= new MealPlanRepository(_context);
    public IRecipeRepository Recipes => _recipeRepository ??= new RecipeRepository(_context);
    public IIngredientRepository Ingredients => _ingredientRepository ??= new IngredientRepository(_context);
    public IOrderRepository Orders => _orderRepository ??= new OrderRepository(_context);
    public INutrientRepository Nutrients => _nutrientRepository ??= new NutrientRepository(_context);
    public IAllergyRepository Allergies => _allergyRepository ??= new AllergyRepository(_context);
    public IStatusRepository Statuses => _statusRepository ??= new StatusRepository(_context);
    public IIngredientAllergyRepository IngredientAllergies => _ingredientAllergyRepository ??= new IngredientAllergyRepository(_context);
    public IHealthProfileRepository HealthProfiles => _healthProfileRepository ??= new HealthProfileRepository(_context);
    public IHealthProfileAllergyRepository HealthProfileAllergies => _healthProfileAllergyRepository ??= new HealthProfileAllergyRepository(_context);
    public IHealthProfileIngredientRepository HealthProfileIngredients => _healthProfileIngredientRepository ??= new HealthProfileIngredientRepository(_context);
    public IFridgeRepository Fridges => _fridgeRepository ??= new FridgeRepository(_context);
    public IFridgeItemRepository FridgeItems => _fridgeItemRepository ??= new FridgeItemRepository(_context);
    public ICartRepository Carts => _cartRepository ??= new CartRepository(_context);
    public ICartItemRepository CartItems => _cartItemRepository ??= new CartItemRepository(_context);
    public IMenuMealRepository MenuMeals => _menuMealRepository ??= new MenuMealRepository(_context);
    public IDailyMenuRepository DailyMenus => _dailyMenuRepository ??= new DailyMenuRepository(_context);
    public IMenuMealRecipeRepository MenuMealRecipes => _menuMealRecipeRepository ??= new MenuMealRecipeRepository(_context);
    public IRecipeIngredientRepository RecipeIngredients => _recipeIngredientRepository ??= new RecipeIngredientRepository(_context);
    public IOrderDetailRepository OrderDetails => _orderDetailRepository ??= new OrderDetailRepository(_context);
    public IDeliveryScheduleRepository DeliverySchedules => _deliveryScheduleRepository ??= new DeliveryScheduleRepository(_context, _dateTimeService);
    public IPaymentGatewayRepository PaymentGateways => _paymentGatewayRepository ??= new PaymentGatewayRepository(_context);
    public ISubscriptionPackageRepository SubscriptionPackages => _subscriptionPackageRepository ??= new SubscriptionPackageRepository(_context);
    public IUserSubscriptionRepository UserSubscriptions => _userSubscriptionRepository ??= new UserSubscriptionRepository(_context);
    public IAICreditPackageRepository AICreditPackages => _aiCreditPackageRepository ??= new AICreditPackageRepository(_context);
    public IAICreditTransactionRepository AICreditTransactions => _aiCreditTransactionRepository ??= new AICreditTransactionRepository(_context);
    public IRevenueReportRepository RevenueReports => _revenueReportRepository ??= new RevenueReportRepository(_context);
    public ISystemConfigurationRepository SystemConfigurations => _systemConfigurationRepository ??= new SystemConfigurationRepository(_context);

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
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
