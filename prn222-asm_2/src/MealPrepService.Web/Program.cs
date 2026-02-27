using Microsoft.EntityFrameworkCore;
using Serilog;
using FluentValidation.AspNetCore;
using FluentValidation;
using MealPrepService.DataAccessLayer.Data;
using MealPrepService.Web.PresentationLayer.Middleware;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using System.Globalization;
using Microsoft.AspNetCore.Localization;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .Build())
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting Meal Prep Service Application");

    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog
    builder.Host.UseSerilog();

    // Add services to the container.
    // Add Razor Pages
    builder.Services.AddRazorPages();

    // Add SignalR
    builder.Services.AddSignalR();

    // Configure Entity Framework Core with SQL Server and SQLite
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    var useSqlite = builder.Configuration.GetValue<bool>("UseSqlite");

    if (useSqlite)
    {
        builder.Services.AddDbContext<MealPrepDbContext>(options =>
            options.UseSqlite(connectionString ?? "Data Source=mealprepservice.db"));
    }
    else
    {
        builder.Services.AddDbContext<MealPrepDbContext>(options =>
            options.UseSqlServer(
                connectionString ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found."),
                sqlServerOptionsAction: sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);
                }));
    }

    // Add FluentValidation
    builder.Services.AddFluentValidationAutoValidation()
        .AddFluentValidationClientsideAdapters()
        .AddValidatorsFromAssemblyContaining<MealPrepService.BusinessLogicLayer.Validators.CreateAccountDtoValidator>();

    // Configure Authentication
    var authBuilder = builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.LoginPath = "/Account/Login";
            options.LogoutPath = "/Account/Logout";
            options.AccessDeniedPath = "/Account/AccessDenied";
            options.ExpireTimeSpan = TimeSpan.FromDays(7);
            options.SlidingExpiration = true;
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.Cookie.SameSite = SameSiteMode.Lax;
        });

    // Add Google OAuth only if credentials are provided
    var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
    var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    
    if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
    {
        authBuilder.AddGoogle(options =>
        {
            options.ClientId = googleClientId;
            options.ClientSecret = googleClientSecret;
        });
    }

    // Configure Authorization
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
        options.AddPolicy("ManagerOnly", policy => policy.RequireRole("Manager"));
        options.AddPolicy("CustomerOnly", policy => policy.RequireRole("Customer"));
        options.AddPolicy("DeliveryManOnly", policy => policy.RequireRole("DeliveryMan"));
        options.AddPolicy("AdminOrManager", policy => policy.RequireRole("Admin", "Manager"));
        options.AddPolicy("CustomerOrManager", policy => policy.RequireRole("Customer", "Manager"));
    });

    builder.Services.AddHttpContextAccessor();

    // Register Data Access Layer dependencies
    builder.Services.AddScoped<MealPrepService.DataAccessLayer.Repositories.IUnitOfWork, MealPrepService.DataAccessLayer.Repositories.UnitOfWork>();
    builder.Services.AddScoped<MealPrepService.DataAccessLayer.Repositories.IAccountRepository, MealPrepService.DataAccessLayer.Repositories.AccountRepository>();
    builder.Services.AddScoped<MealPrepService.DataAccessLayer.Repositories.IUserSubscriptionRepository, MealPrepService.DataAccessLayer.Repositories.UserSubscriptionRepository>();
    builder.Services.AddScoped<MealPrepService.DataAccessLayer.Repositories.IMealPlanRepository, MealPrepService.DataAccessLayer.Repositories.MealPlanRepository>();
    builder.Services.AddScoped<MealPrepService.DataAccessLayer.Repositories.IRecipeRepository, MealPrepService.DataAccessLayer.Repositories.RecipeRepository>();
    builder.Services.AddScoped<MealPrepService.DataAccessLayer.Repositories.IOrderRepository, MealPrepService.DataAccessLayer.Repositories.OrderRepository>();
    builder.Services.AddScoped<MealPrepService.DataAccessLayer.Repositories.IDailyMenuRepository, MealPrepService.DataAccessLayer.Repositories.DailyMenuRepository>();
    builder.Services.AddScoped<MealPrepService.DataAccessLayer.Repositories.IFridgeItemRepository, MealPrepService.DataAccessLayer.Repositories.FridgeItemRepository>();

    // Register Business Logic Layer dependencies
    builder.Services.AddScoped<MealPrepService.BusinessLogicLayer.Interfaces.IPasswordHasher, MealPrepService.BusinessLogicLayer.Services.PasswordHasher>();
    builder.Services.AddScoped<MealPrepService.BusinessLogicLayer.Interfaces.IAccountService, MealPrepService.BusinessLogicLayer.Services.AccountService>();
    builder.Services.AddScoped<MealPrepService.BusinessLogicLayer.Interfaces.IHealthProfileService, MealPrepService.BusinessLogicLayer.Services.HealthProfileService>();
    builder.Services.AddScoped<MealPrepService.BusinessLogicLayer.Interfaces.IMealPlanService, MealPrepService.BusinessLogicLayer.Services.MealPlanService>();
    builder.Services.AddScoped<MealPrepService.BusinessLogicLayer.Interfaces.IFridgeService, MealPrepService.BusinessLogicLayer.Services.FridgeService>();
    builder.Services.AddScoped<MealPrepService.BusinessLogicLayer.Interfaces.IMenuService, MealPrepService.BusinessLogicLayer.Services.MenuService>();
    builder.Services.AddScoped<MealPrepService.BusinessLogicLayer.Interfaces.IOrderService, MealPrepService.BusinessLogicLayer.Services.OrderService>();
    builder.Services.AddScoped<MealPrepService.BusinessLogicLayer.Interfaces.IDeliveryService, MealPrepService.BusinessLogicLayer.Services.DeliveryService>();
    builder.Services.AddScoped<MealPrepService.BusinessLogicLayer.Interfaces.IRecipeService, MealPrepService.BusinessLogicLayer.Services.RecipeService>();
    builder.Services.AddScoped<MealPrepService.BusinessLogicLayer.Interfaces.IIngredientService, MealPrepService.BusinessLogicLayer.Services.IngredientService>();
    builder.Services.AddScoped<MealPrepService.BusinessLogicLayer.Interfaces.IAllergyService, MealPrepService.BusinessLogicLayer.Services.AllergyService>();
    builder.Services.AddScoped<MealPrepService.BusinessLogicLayer.Interfaces.IRevenueService, MealPrepService.BusinessLogicLayer.Services.RevenueService>();
    builder.Services.AddScoped<MealPrepService.BusinessLogicLayer.Interfaces.IVnpayService, MealPrepService.BusinessLogicLayer.Services.VnpayService>();
    builder.Services.AddScoped<MealPrepService.BusinessLogicLayer.Interfaces.ISystemConfigurationService, MealPrepService.BusinessLogicLayer.Services.SystemConfigurationService>();
    
    // Add HTTP client factory for AI services
    builder.Services.AddHttpClient();
    
    // Register AI Services
    builder.Services.AddScoped<MealPrepService.BusinessLogicLayer.Interfaces.IAIOperationLogger, MealPrepService.BusinessLogicLayer.Services.AIOperationLogger>();
    builder.Services.AddScoped<MealPrepService.BusinessLogicLayer.Interfaces.IAIConfigurationService, MealPrepService.BusinessLogicLayer.Services.AIConfigurationService>();
    builder.Services.AddScoped<MealPrepService.BusinessLogicLayer.Interfaces.ICustomerProfileAnalyzer, MealPrepService.BusinessLogicLayer.Services.CustomerProfileAnalyzer>();
    
    // Register LLM service (OpenAI GPT)
    builder.Services.AddScoped<MealPrepService.BusinessLogicLayer.Interfaces.ILLMService, MealPrepService.BusinessLogicLayer.Services.OpenAIRecommendationService>();
    
    // Register AI-only Recommendation Engine
    builder.Services.AddScoped<MealPrepService.BusinessLogicLayer.Interfaces.IRecommendationEngine, MealPrepService.BusinessLogicLayer.Services.AIRecommendationEngine>();
    builder.Services.AddScoped<MealPrepService.BusinessLogicLayer.Interfaces.IAIRecommendationService, MealPrepService.BusinessLogicLayer.Services.AIRecommendationService>();

    var app = builder.Build();

    // Configure localization to use English (US)
    var supportedCultures = new[] { new CultureInfo("en-US") };
    app.UseRequestLocalization(new RequestLocalizationOptions
    {
        DefaultRequestCulture = new RequestCulture("en-US"),
        SupportedCultures = supportedCultures,
        SupportedUICultures = supportedCultures
    });

    // Seed database
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<MealPrepDbContext>();
            var logger = services.GetRequiredService<ILogger<MealPrepService.Web.Data.DbSeeder>>();
            var passwordHasher = services.GetRequiredService<MealPrepService.BusinessLogicLayer.Interfaces.IPasswordHasher>();
            
            // Get the files directory path - use absolute path from content root
            var filesDirectory = Path.Combine(app.Environment.ContentRootPath, "..", "..", "files");
            filesDirectory = Path.GetFullPath(filesDirectory); // Normalize the path
            
            var seeder = new MealPrepService.Web.Data.DbSeeder(context, logger, passwordHasher, filesDirectory);
            await seeder.SeedAsync();
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while seeding the database");
        }
    }

    // Configure the HTTP request pipeline.
    
    // Add global exception handler (should be early in the pipeline)
    app.UseGlobalExceptionHandler();

    if (!app.Environment.IsDevelopment())
    {
        // Remove the default exception handler since we have our global one
        // app.UseExceptionHandler("/Home/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    // Add Serilog request logging
    app.UseSerilogRequestLogging();

    app.UseHttpsRedirection();
    app.UseRouting();

    // Add authentication and authorization middleware
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapStaticAssets();

    // Map Razor Pages
    app.MapRazorPages();
    
    // Map SignalR Hubs
    app.MapHub<MealPrepService.Web.Hubs.OrderHub>("/hubs/order");
    app.MapHub<MealPrepService.Web.Hubs.DeliveryHub>("/hubs/delivery");
    app.MapHub<MealPrepService.Web.Hubs.MenuHub>("/hubs/menu");
    app.MapHub<MealPrepService.Web.Hubs.NotificationHub>("/hubs/notification");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
