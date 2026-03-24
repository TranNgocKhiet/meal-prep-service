using MealPreparationService.DataAccess.Repositories;

namespace MealPreparationService.DataAccess.UnitOfWork;

public interface IUnitOfWork : IDisposable
{
    IAccountRepository Accounts { get; }
    IRoleRepository Roles { get; }
    IGoogleAuthRepository GoogleAuths { get; }
    IMealPlanRepository MealPlans { get; }
    IRecipeRepository Recipes { get; }
    IIngredientRepository Ingredients { get; }
    IOrderRepository Orders { get; }
    INutrientRepository Nutrients { get; }
    IAllergyRepository Allergies { get; }
    IStatusRepository Statuses { get; }
    IIngredientAllergyRepository IngredientAllergies { get; }
    IHealthProfileRepository HealthProfiles { get; }
    IHealthProfileAllergyRepository HealthProfileAllergies { get; }
    IHealthProfileIngredientRepository HealthProfileIngredients { get; }
    IFridgeRepository Fridges { get; }
    IFridgeItemRepository FridgeItems { get; }
    ICartRepository Carts { get; }
    ICartItemRepository CartItems { get; }
    IMenuMealRepository MenuMeals { get; }
    IDailyMenuRepository DailyMenus { get; }
    IMenuMealRecipeRepository MenuMealRecipes { get; }
    IRecipeIngredientRepository RecipeIngredients { get; }
    IOrderDetailRepository OrderDetails { get; }
    IDeliveryScheduleRepository DeliverySchedules { get; }
    IPaymentGatewayRepository PaymentGateways { get; }
    ISubscriptionPackageRepository SubscriptionPackages { get; }
    IUserSubscriptionRepository UserSubscriptions { get; }
    IAICreditPackageRepository AICreditPackages { get; }
    IAICreditTransactionRepository AICreditTransactions { get; }
    IRevenueReportRepository RevenueReports { get; }
    ISystemConfigurationRepository SystemConfigurations { get; }    IFeedbackRepository Feedbacks { get; }    
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
