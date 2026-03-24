using MealPreparationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MealPreparationService.DataAccess.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Account> Accounts { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Status> Statuses { get; set; }
    public DbSet<MealType> MealTypes { get; set; }
    public DbSet<RelationshipType> RelationshipTypes { get; set; }
    public DbSet<GoogleAuth> GoogleAuths { get; set; }
    public DbSet<MealPlan> MealPlans { get; set; }
    public DbSet<Meal> Meals { get; set; }
    public DbSet<MealRecipe> MealRecipes { get; set; }
    public DbSet<Recipe> Recipes { get; set; }
    public DbSet<Ingredient> Ingredients { get; set; }
    public DbSet<RecipeIngredient> RecipeIngredients { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<Nutrient> Nutrients { get; set; }
    public DbSet<IngredientNutrient> IngredientNutrients { get; set; }
    public DbSet<Allergy> Allergies { get; set; }
    public DbSet<IngredientAllergy> IngredientAllergies { get; set; }
    public DbSet<SystemConfiguration> SystemConfigurations { get; set; }
    public DbSet<HealthProfile> HealthProfiles { get; set; }
    public DbSet<HealthProfileAllergy> HealthProfileAllergies { get; set; }
    public DbSet<HealthProfileIngredient> HealthProfileIngredients { get; set; }
    public DbSet<Fridge> Fridges { get; set; }
    public DbSet<FridgeItem> FridgeItems { get; set; }
    public DbSet<Cart> Carts { get; set; }
    public DbSet<CartItem> CartItems { get; set; }
    public DbSet<MenuMeal> MenuMeals { get; set; }
    public DbSet<DailyMenu> DailyMenus { get; set; }
    public DbSet<MenuMealRecipe> MenuMealRecipes { get; set; }
    public DbSet<OrderDetail> OrderDetails { get; set; }
    public DbSet<DeliverySchedule> DeliverySchedules { get; set; }
    public DbSet<PaymentGateway> PaymentGateways { get; set; }
    public DbSet<SubscriptionPackage> SubscriptionPackages { get; set; }
    public DbSet<UserSubscription> UserSubscriptions { get; set; }
    public DbSet<AIcreditPackage> AIcreditPackages { get; set; }
    public DbSet<AIcreditTransaction> AIcreditTransactions { get; set; }
    public DbSet<AIServiceUsageLog> AIServiceUsageLogs { get; set; }
    public DbSet<RevenueReport> RevenueReports { get; set; }
    public DbSet<Feedback> Feedbacks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureAccount(modelBuilder);
        ConfigureRole(modelBuilder);
        ConfigureStatus(modelBuilder);
        ConfigureMealType(modelBuilder);
        ConfigureRelationshipType(modelBuilder);
        ConfigureGoogleAuth(modelBuilder);
        ConfigureMealPlan(modelBuilder);
        ConfigureMeal(modelBuilder);
        ConfigureMealRecipe(modelBuilder);
        ConfigureRecipe(modelBuilder);
        ConfigureIngredient(modelBuilder);
        ConfigureRecipeIngredient(modelBuilder);
        ConfigureOrder(modelBuilder);
        ConfigureNutrient(modelBuilder);
        ConfigureIngredientNutrient(modelBuilder);
        ConfigureAllergy(modelBuilder);
        ConfigureIngredientAllergy(modelBuilder);
        ConfigureSystemConfiguration(modelBuilder);
        ConfigureHealthProfile(modelBuilder);
        ConfigureHealthProfileAllergy(modelBuilder);
        ConfigureHealthProfileIngredient(modelBuilder);
        ConfigureFridge(modelBuilder);
        ConfigureFridgeItem(modelBuilder);
        ConfigureCart(modelBuilder);
        ConfigureCartItem(modelBuilder);
        ConfigureMenuMeal(modelBuilder);
        ConfigureDailyMenu(modelBuilder);
        ConfigureMenuMealRecipe(modelBuilder);
        ConfigureOrderDetail(modelBuilder);
        ConfigureDeliverySchedule(modelBuilder);
        ConfigurePaymentGateway(modelBuilder);
        ConfigureSubscriptionPackage(modelBuilder);
        ConfigureUserSubscription(modelBuilder);
        ConfigureAIcreditPackage(modelBuilder);
        ConfigureAIcreditTransaction(modelBuilder);
        ConfigureAIServiceUsageLog(modelBuilder);
        ConfigureRevenueReport(modelBuilder);
        ConfigureFeedback(modelBuilder);
    }

    private void ConfigureAccount(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.CurrentCredits).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            entity.Property(e => e.IsActive).IsRequired();
            
            entity.HasOne(e => e.Role)
                .WithMany(r => r.Accounts)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.GoogleAuth)
                .WithMany(g => g.Accounts)
                .HasForeignKey(e => e.GoogleAuthId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureRole(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever(); // Manual ID assignment for int
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
        });
    }

    private void ConfigureStatus(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Status>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever(); // Manual ID assignment for int
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
        });
    }

    private void ConfigureMealType(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MealType>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever(); // Manual ID assignment for int enum
            entity.Property(e => e.TypeName).IsRequired().HasMaxLength(20);
        });
    }

    private void ConfigureRelationshipType(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RelationshipType>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever(); // Manual ID assignment for int enum
            entity.Property(e => e.TypeName).IsRequired().HasMaxLength(20);
        });
    }

    private void ConfigureGoogleAuth(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GoogleAuth>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProviderKey).IsRequired().HasMaxLength(255);
            entity.Property(e => e.AccessToken).IsRequired().HasMaxLength(255);
            entity.Property(e => e.RefreshToken).IsRequired().HasMaxLength(255);
            entity.Property(e => e.ExpiresAt).IsRequired();
            entity.Property(e => e.IsVerified).IsRequired();
        });
    }

    private void ConfigureMealPlan(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MealPlan>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PlanName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.StartDate).IsRequired();
            entity.Property(e => e.EndDate).IsRequired();
            entity.Property(e => e.IsAiGenerated).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            entity.Property(e => e.IsActive).IsRequired();
            
            entity.HasOne(e => e.Account)
                .WithMany(u => u.MealPlans)
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureMeal(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Meal>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TotalCalories).HasPrecision(18, 2);
            entity.Property(e => e.ProteinG).HasPrecision(18, 2);
            entity.Property(e => e.FatG).HasPrecision(18, 2);
            entity.Property(e => e.CarbsG).HasPrecision(18, 2);
            entity.Property(e => e.ServerDate).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            entity.Property(e => e.MealFinished).IsRequired();
            
            entity.HasOne(e => e.MealPlan)
                .WithMany(mp => mp.Meals)
                .HasForeignKey(e => e.PlanId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.MealType)
                .WithMany(mt => mt.Meals)
                .HasForeignKey(e => e.MealTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureMealRecipe(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MealRecipe>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.Meal)
                .WithMany(m => m.MealRecipes)
                .HasForeignKey(e => e.MealId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Recipe)
                .WithMany(r => r.MealRecipes)
                .HasForeignKey(e => e.RecipeId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureRecipe(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Recipe>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RecipeName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Instructions).HasMaxLength(2000);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
        });
    }

    private void ConfigureIngredient(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Ingredient>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Unit).HasMaxLength(50);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
        });
    }

    private void ConfigureRecipeIngredient(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RecipeIngredient>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasPrecision(18, 2).IsRequired();
            
            // Check constraint for Amount > 0
            entity.ToTable(t => t.HasCheckConstraint("CK_RecipeIngredient_Amount", "[Amount] > 0"));
            
            entity.HasOne(e => e.Recipe)
                .WithMany(r => r.RecipeIngredients)
                .HasForeignKey(e => e.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Ingredient)
                .WithMany(i => i.RecipeIngredients)
                .HasForeignKey(e => e.IngredientId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureOrder(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Date).IsRequired();
            entity.Property(e => e.Amount).HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.PaymentMethod).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            
            // Check constraint for Amount >= 0
            entity.ToTable(t => t.HasCheckConstraint("CK_Order_Amount", "[Amount] >= 0"));
            
            entity.HasOne(e => e.Customer)
                .WithMany(u => u.Orders)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.Status)
                .WithMany(s => s.Orders)
                .HasForeignKey(e => e.StatusId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.PaymentGateway)
                .WithMany(pg => pg.Orders)
                .HasForeignKey(e => e.PaymentGatewayId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.ConfirmedByAccount)
                .WithMany()
                .HasForeignKey(e => e.OrderConfirmedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureNutrient(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Nutrient>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
        });
    }

    private void ConfigureIngredientNutrient(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IngredientNutrient>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AmountPer100).HasPrecision(18, 2);
            
            entity.HasOne(e => e.Ingredient)
                .WithMany(i => i.IngredientNutrients)
                .HasForeignKey(e => e.IngredientId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Nutrient)
                .WithMany(n => n.IngredientNutrients)
                .HasForeignKey(e => e.NutrientId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureAllergy(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Allergy>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
        });
    }

    private void ConfigureIngredientAllergy(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IngredientAllergy>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.Ingredient)
                .WithMany(i => i.IngredientAllergies)
                .HasForeignKey(e => e.IngredientId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Allergy)
                .WithMany(a => a.IngredientAllergies)
                .HasForeignKey(e => e.AllergyId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureSystemConfiguration(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SystemConfiguration>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Key).IsUnique();
            entity.Property(e => e.Key).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Value).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.DataType).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(500);
        });
    }

    private void ConfigureHealthProfile(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<HealthProfile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Gender).HasMaxLength(50);
            entity.Property(e => e.HealthNotes).HasMaxLength(2000);
            entity.Property(e => e.Weight).HasPrecision(18, 2);
            entity.Property(e => e.Height).HasPrecision(18, 2);
            
            entity.HasOne(e => e.Account)
                .WithMany(a => a.HealthProfiles)
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureHealthProfileAllergy(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<HealthProfileAllergy>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.HealthProfile)
                .WithMany(hp => hp.HealthProfileAllergies)
                .HasForeignKey(e => e.HealthProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Allergy)
                .WithMany(a => a.HealthProfileAllergies)
                .HasForeignKey(e => e.AllergyId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureHealthProfileIngredient(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<HealthProfileIngredient>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.HealthProfile)
                .WithMany(hp => hp.HealthProfileIngredients)
                .HasForeignKey(e => e.HealthProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Ingredient)
                .WithMany(i => i.HealthProfileIngredients)
                .HasForeignKey(e => e.IngredientId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.RelationshipType)
                .WithMany(rt => rt.HealthProfileIngredients)
                .HasForeignKey(e => e.RelationshipTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureFridge(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Fridge>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.Account)
                .WithMany(a => a.Fridges)
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureFridgeItem(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FridgeItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CurrentAmount).HasPrecision(18, 2);
            
            entity.HasOne(e => e.Fridge)
                .WithMany(f => f.FridgeItems)
                .HasForeignKey(e => e.FridgeId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Account)
                .WithMany()
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.Ingredient)
                .WithMany()
                .HasForeignKey(e => e.IngredientId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureCart(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.Account)
                .WithMany(a => a.Carts)
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureCartItem(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Quantity).IsRequired();
            
            entity.ToTable(t => t.HasCheckConstraint("CK_CartItem_Quantity", "[Quantity] > 0"));
            
            entity.HasOne(e => e.Cart)
                .WithMany(c => c.CartItems)
                .HasForeignKey(e => e.CartId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.MenuMeal)
                .WithMany(mm => mm.CartItems)
                .HasForeignKey(e => e.MenuMealId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureMenuMeal(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MenuMeal>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TotalCalories).HasPrecision(18, 2);
            entity.Property(e => e.ProteinG).HasPrecision(18, 2);
            entity.Property(e => e.FatG).HasPrecision(18, 2);
            entity.Property(e => e.CarbsG).HasPrecision(18, 2);
            entity.Property(e => e.Price).HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.AvailableQuantity).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            
            entity.ToTable(t => t.HasCheckConstraint("CK_MenuMeal_Price", "[Price] >= 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_MenuMeal_AvailableQuantity", "[AvailableQuantity] >= 0"));
            
            entity.HasOne(e => e.Menu)
                .WithMany(dm => dm.MenuMeals)
                .HasForeignKey(e => e.MenuId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.MealType)
                .WithMany(mt => mt.MenuMeals)
                .HasForeignKey(e => e.MealTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureDailyMenu(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DailyMenu>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MenuDate).IsRequired();
            
            entity.HasOne(e => e.Status)
                .WithMany(s => s.DailyMenus)
                .HasForeignKey(e => e.StatusId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureMenuMealRecipe(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MenuMealRecipe>(entity =>
        {
            // Composite key for join table
            entity.HasKey(e => new { e.MenuMealId, e.RecipeId });
            
            entity.HasOne(e => e.MenuMeal)
                .WithMany(mm => mm.MenuMealRecipes)
                .HasForeignKey(e => e.MenuMealId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Recipe)
                .WithMany(r => r.MenuMealRecipes)
                .HasForeignKey(e => e.RecipeId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureOrderDetail(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrderDetail>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Quantity).IsRequired();
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            
            entity.ToTable(t => t.HasCheckConstraint("CK_OrderDetail_Quantity", "[Quantity] > 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_OrderDetail_UnitPrice", "[UnitPrice] >= 0"));
            
            entity.HasOne(e => e.Order)
                .WithMany(o => o.OrderDetails)
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.MenuMeal)
                .WithMany(mm => mm.OrderDetails)
                .HasForeignKey(e => e.MenuMealId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureDeliverySchedule(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DeliverySchedule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DeliveryTime).IsRequired();
            entity.Property(e => e.Address).IsRequired().HasMaxLength(255);
            entity.Property(e => e.DriverContact).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            
            entity.HasOne(e => e.Driver)
                .WithMany(a => a.DeliverySchedules)
                .HasForeignKey(e => e.DriverId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.Order)
                .WithMany(o => o.DeliverySchedules)
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigurePaymentGateway(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PaymentGateway>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TransactionNo).IsRequired().HasMaxLength(200);
            entity.Property(e => e.BankCode).HasMaxLength(50);
            entity.Property(e => e.ResponseCode).HasMaxLength(50);
            entity.Property(e => e.PayDate).IsRequired();
            
            entity.HasOne(e => e.Status)
                .WithMany(s => s.PaymentGateways)
                .HasForeignKey(e => e.StatusId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureSubscriptionPackage(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SubscriptionPackage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PackageName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Price).HasPrecision(18, 2);
            entity.Property(e => e.Description).HasMaxLength(1000);
            
            entity.ToTable(t => t.HasCheckConstraint("CK_SubscriptionPackage_Price", "[Price] >= 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_SubscriptionPackage_DurationDays", "[DurationDays] > 0"));
        });
    }

    private void ConfigureUserSubscription(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserSubscription>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.StartDate).IsRequired();
            entity.Property(e => e.EndDate).IsRequired();
            
            entity.HasOne(e => e.Account)
                .WithMany(a => a.UserSubscriptions)
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.SubscriptionPackage)
                .WithMany(sp => sp.UserSubscriptions)
                .HasForeignKey(e => e.SubscriptionPackageId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.PaymentGateway)
                .WithMany(pg => pg.UserSubscriptions)
                .HasForeignKey(e => e.PaymentGatewayId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureAIcreditPackage(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AIcreditPackage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PackageName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Price).HasPrecision(18, 2);
            
            entity.ToTable(t => t.HasCheckConstraint("CK_AIcreditPackage_Price", "[Price] >= 0"));
        });
    }

    private void ConfigureAIcreditTransaction(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AIcreditTransaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CreatedAt).IsRequired();
            
            entity.HasOne(e => e.Account)
                .WithMany(a => a.AIcreditTransactions)
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.AIcreditPackage)
                .WithMany(acp => acp.AIcreditTransactions)
                .HasForeignKey(e => e.AIcreditPackageId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.PaymentGateway)
                .WithMany(pg => pg.AIcreditTransactions)
                .HasForeignKey(e => e.PaymentGatewayId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureAIServiceUsageLog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AIServiceUsageLog>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.OperationType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Timestamp).IsRequired();
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.InputParameters);
            entity.Property(e => e.OutputSummary);
            entity.Property(e => e.ErrorMessage);
            entity.Property(e => e.StackTrace);
            entity.Property(e => e.ExecutionDurationMs).IsRequired();
            entity.Property(e => e.CreditsUsed).IsRequired();

            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => new { e.OperationType, e.Timestamp });

            entity.HasOne(e => e.Customer)
                .WithMany(a => a.AIServiceUsageLogs)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureRevenueReport(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RevenueReport>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Month).IsRequired();
            entity.Property(e => e.Year).IsRequired();
            entity.Property(e => e.TotalSubscriptionRev).HasPrecision(18, 2);
            entity.Property(e => e.TotalOrderRev).HasPrecision(18, 2);
            entity.Property(e => e.TotalAiCreditRev).HasPrecision(18, 2);
            
            entity.ToTable(t => t.HasCheckConstraint("CK_RevenueReport_Month", "[Month] >= 1 AND [Month] <= 12"));
            entity.ToTable(t => t.HasCheckConstraint("CK_RevenueReport_Year", "[Year] > 0"));
        });
    }

    private void ConfigureFeedback(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CustomerId).IsRequired();
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            
            entity.HasOne(e => e.Customer)
                .WithMany(a => a.Feedbacks)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => e.CustomerId);
        });
    }
}
