using MealPreparationService.API.Middleware;
using MealPreparationService.Business.Services;
using MealPreparationService.DataAccess.Data;
using MealPreparationService.DataAccess.Repositories;
using MealPreparationService.DataAccess.UnitOfWork;
using MealPreparationService.DataAccess.Interceptors;
using MealPreparationService.Domain.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OfficeOpenXml;
using System.Text;
using Serilog;
using Serilog.Events;
using OpenAiServiceImpl = MealPreparationService.Business.Services.OpenAiIntegrationService;

// Set EPPlus license for EPPlus 8+
// For non-commercial use (educational/personal projects)
ExcelPackage.License.SetNonCommercialOrganization("SWD392 Educational Project");

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog for structured logging
// Validates: Requirements 21.1, 21.2, 21.3, 21.4, 21.5, 21.6
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "MealPreparationService")
    .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/app-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
        shared: true)
    .WriteTo.File(
        path: "logs/errors-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        restrictedToMinimumLevel: LogEventLevel.Error,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
        shared: true)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        // Customize automatic model validation response
        options.InvalidModelStateResponseFactory = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            
            var errors = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                );
            
            logger.LogWarning("Model validation failed for {Path}: {Errors}", 
                context.HttpContext.Request.Path, 
                System.Text.Json.JsonSerializer.Serialize(errors));
            
            var errorMessage = string.Join("; ", errors.SelectMany(e => e.Value));
            
            return new BadRequestObjectResult(new
            {
                success = false,
                message = errorMessage,
                errors = errors
            });
        };
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Database
builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
{
    var logger = serviceProvider.GetRequiredService<ILogger<QueryPerformanceInterceptor>>();
    var interceptor = new QueryPerformanceInterceptor(logger);
    
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"))
        .AddInterceptors(interceptor);
});

// Register Unit of Work (Scoped) - provides access to all repositories
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Register Repositories (Scoped)
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IStatusRepository, StatusRepository>();
builder.Services.AddScoped<IAllergyRepository, AllergyRepository>();
builder.Services.AddScoped<IIngredientAllergyRepository, IngredientAllergyRepository>();
builder.Services.AddScoped<IIngredientRepository, IngredientRepository>();
builder.Services.AddScoped<INutrientRepository, NutrientRepository>();
builder.Services.AddScoped<IRecipeRepository, RecipeRepository>();
builder.Services.AddScoped<IMealPlanRepository, MealPlanRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IHealthProfileRepository, HealthProfileRepository>();
builder.Services.AddScoped<IHealthProfileAllergyRepository, HealthProfileAllergyRepository>();
builder.Services.AddScoped<IHealthProfileIngredientRepository, HealthProfileIngredientRepository>();
builder.Services.AddScoped<IFridgeRepository, FridgeRepository>();
builder.Services.AddScoped<IFridgeItemRepository, FridgeItemRepository>();
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<ICartItemRepository, CartItemRepository>();
builder.Services.AddScoped<IDailyMenuRepository, DailyMenuRepository>();
builder.Services.AddScoped<IMenuMealRepository, MenuMealRepository>();
builder.Services.AddScoped<IMenuMealRecipeRepository, MenuMealRecipeRepository>();
builder.Services.AddScoped<IOrderDetailRepository, OrderDetailRepository>();
builder.Services.AddScoped<IDeliveryScheduleRepository, DeliveryScheduleRepository>();
builder.Services.AddScoped<IPaymentGatewayRepository, PaymentGatewayRepository>();
builder.Services.AddScoped<ISubscriptionPackageRepository, SubscriptionPackageRepository>();
builder.Services.AddScoped<IUserSubscriptionRepository, UserSubscriptionRepository>();
builder.Services.AddScoped<IAICreditPackageRepository, AICreditPackageRepository>();
builder.Services.AddScoped<IAICreditTransactionRepository, AICreditTransactionRepository>();
builder.Services.AddScoped<IRevenueReportRepository, RevenueReportRepository>();
builder.Services.AddScoped<ISystemConfigurationRepository, SystemConfigurationRepository>();

// Register Business Services (Scoped)
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IGoogleOAuthService, GoogleOAuthService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IExcelReaderService, ExcelReaderService>();
builder.Services.AddScoped<IDatasetImportService, DatasetImportService>();
builder.Services.AddScoped<IOpenAiService, OpenAiServiceImpl>();
builder.Services.AddScoped<IGoogleMapsService, GoogleMapsService>();
builder.Services.AddScoped<IDeliveryDistanceService, DeliveryDistanceService>();
builder.Services.AddScoped<IVnPayService, VnPayService>();
builder.Services.AddScoped<IIngredientService, IngredientService>();
builder.Services.AddScoped<ILogSanitizerService, LogSanitizerService>();
builder.Services.AddScoped<IMealPlanService, MealPlanService>();
builder.Services.AddScoped<IAIMealPlanService, AIMealPlanService>();
builder.Services.AddScoped<IAllergyService, AllergyService>();
builder.Services.AddScoped<IRecipeService, RecipeService>();
builder.Services.AddScoped<IMealTrackingService, MealTrackingService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IVirtualFridgeService, VirtualFridgeService>();
builder.Services.AddScoped<IAICreditPackageService, AICreditPackageService>();
builder.Services.AddScoped<ISubscriptionPackageService, SubscriptionPackageService>();
builder.Services.AddScoped<IAICreditTransactionService, AICreditTransactionService>();
builder.Services.AddSingleton<IDateTimeService, DateTimeService>();
// TODO: Implement these services
// builder.Services.AddScoped<IGroceryListService, GroceryListService>();
// builder.Services.AddScoped<IAllergyCheckService, AllergyCheckService>();
// builder.Services.AddScoped<INutrientCalculatorService, NutrientCalculatorService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IDeliveryScheduleService, DeliveryScheduleService>();
// builder.Services.AddScoped<IDeliveryService, DeliveryService>();
// builder.Services.AddScoped<IUserDataService, UserDataService>();

// Register HttpClient for Google Maps
builder.Services.AddHttpClient("GoogleMaps");

// Register Singleton Services
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICacheService, CacheService>();

// Register Hosted Services (Background Services)
builder.Services.AddHostedService<DatasetImportHostedService>();

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CustomerOnly", policy => 
        policy.RequireRole("Customer"));
    
    options.AddPolicy("StaffOnly", policy => 
        policy.RequireRole("Admin", "Staff"));
    
    options.AddPolicy("DeliveryPersonnelOnly", policy => 
        policy.RequireRole("Delivery_Personnel", "DeliveryMan"));
    
    options.AddPolicy("StaffOrDeliveryPersonnel", policy => 
        policy.RequireRole("Admin", "Staff", "Delivery_Personnel", "DeliveryMan"));
    
    options.AddPolicy("AllRoles", policy => 
        policy.RequireRole("Customer", "Admin", "Staff", "Manager", "Delivery_Personnel", "DeliveryMan"));
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(builder.Configuration.GetSection("CorsOrigins").Get<string[]>() ?? new[] { "http://localhost:3000" })
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Logging is configured via Serilog above

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Add custom middleware
app.UseMiddleware<PerformanceMonitoringMiddleware>();
app.UseMiddleware<InputSanitizationMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<RoleAuthorizationMiddleware>();

app.UseHttpsRedirection();

// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
    await next();
});

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Seed database with initial data (admin account, etc.)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var unitOfWork = services.GetRequiredService<IUnitOfWork>();
        var logger = services.GetRequiredService<ILogger<MealPreparationService.DataAccess.Data.DatabaseSeeder>>();
        var dateTimeService = services.GetRequiredService<IDateTimeService>();
        var seeder = new MealPreparationService.DataAccess.Data.DatabaseSeeder(unitOfWork, logger, dateTimeService);
        await seeder.SeedAsync();
        Log.Information("Database seeding completed successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while seeding the database");
    }
}

try
{
    Log.Information("Starting Meal Preparation Service");
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
