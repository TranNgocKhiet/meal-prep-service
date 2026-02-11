using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.Exceptions;
using MealPrepService.DataAccessLayer.Repositories;
using MealPrepService.DataAccessLayer.Entities;
using Microsoft.Extensions.Logging;

namespace MealPrepService.BusinessLogicLayer.Services;

public class SystemConfigurationService : ISystemConfigurationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SystemConfigurationService> _logger;
    
    private const string MAX_MEAL_PLANS_KEY = "MaxMealPlansPerCustomer";
    private const string MAX_FRIDGE_ITEMS_KEY = "MaxFridgeItemsPerCustomer";
    private const string MAX_MEAL_PLAN_DAYS_KEY = "MaxMealPlanDays";
    private const int DEFAULT_MAX_MEAL_PLANS = 5;
    private const int DEFAULT_MAX_FRIDGE_ITEMS = 100;
    private const int DEFAULT_MAX_MEAL_PLAN_DAYS = 7;

    public SystemConfigurationService(
        IUnitOfWork unitOfWork,
        ILogger<SystemConfigurationService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<int> GetMaxMealPlansPerCustomerAsync()
    {
        var config = await GetConfigurationAsync(MAX_MEAL_PLANS_KEY);
        if (config == null)
        {
            await InitializeDefaultConfigurationsAsync();
            return DEFAULT_MAX_MEAL_PLANS;
        }
        
        return int.TryParse(config.ConfigValue, out var value) ? value : DEFAULT_MAX_MEAL_PLANS;
    }

    public async Task<int> GetMaxFridgeItemsPerCustomerAsync()
    {
        var config = await GetConfigurationAsync(MAX_FRIDGE_ITEMS_KEY);
        if (config == null)
        {
            await InitializeDefaultConfigurationsAsync();
            return DEFAULT_MAX_FRIDGE_ITEMS;
        }
        
        return int.TryParse(config.ConfigValue, out var value) ? value : DEFAULT_MAX_FRIDGE_ITEMS;
    }

    public async Task<int> GetMaxMealPlanDaysAsync()
    {
        var config = await GetConfigurationAsync(MAX_MEAL_PLAN_DAYS_KEY);
        if (config == null)
        {
            await InitializeDefaultConfigurationsAsync();
            return DEFAULT_MAX_MEAL_PLAN_DAYS;
        }
        
        return int.TryParse(config.ConfigValue, out var value) ? value : DEFAULT_MAX_MEAL_PLAN_DAYS;
    }

    public async Task UpdateMaxMealPlansAsync(int maxValue, string updatedBy)
    {
        if (maxValue < 1)
        {
            throw new BusinessException("Maximum meal plans must be at least 1");
        }

        if (maxValue > 100)
        {
            throw new BusinessException("Maximum meal plans cannot exceed 100");
        }

        await UpdateConfigurationAsync(
            MAX_MEAL_PLANS_KEY, 
            maxValue.ToString(), 
            "Maximum number of meal plans each customer can have",
            updatedBy);
        
        _logger.LogInformation("Max meal plans updated to {MaxValue} by {UpdatedBy}", maxValue, updatedBy);
    }

    public async Task UpdateMaxFridgeItemsAsync(int maxValue, string updatedBy)
    {
        if (maxValue < 1)
        {
            throw new BusinessException("Maximum fridge items must be at least 1");
        }

        if (maxValue > 1000)
        {
            throw new BusinessException("Maximum fridge items cannot exceed 1000");
        }

        await UpdateConfigurationAsync(
            MAX_FRIDGE_ITEMS_KEY, 
            maxValue.ToString(), 
            "Maximum number of items each customer can have in their fridge",
            updatedBy);
        
        _logger.LogInformation("Max fridge items updated to {MaxValue} by {UpdatedBy}", maxValue, updatedBy);
    }

    public async Task UpdateMaxMealPlanDaysAsync(int maxDays, string updatedBy)
    {
        if (maxDays < 1)
        {
            throw new BusinessException("Maximum meal plan days must be at least 1");
        }

        if (maxDays > 30)
        {
            throw new BusinessException("Maximum meal plan days cannot exceed 30");
        }

        await UpdateConfigurationAsync(
            MAX_MEAL_PLAN_DAYS_KEY, 
            maxDays.ToString(), 
            "Maximum number of days for a meal plan",
            updatedBy);
        
        _logger.LogInformation("Max meal plan days updated to {MaxDays} by {UpdatedBy}", maxDays, updatedBy);
    }

    public async Task<Dictionary<string, string>> GetAllConfigurationsAsync()
    {
        var configs = await _unitOfWork.SystemConfigurations.GetAllAsync();
        return configs.ToDictionary(c => c.ConfigKey, c => c.ConfigValue);
    }

    public async Task InitializeDefaultConfigurationsAsync()
    {
        var existingConfigs = await _unitOfWork.SystemConfigurations.GetAllAsync();
        
        if (!existingConfigs.Any(c => c.ConfigKey == MAX_MEAL_PLANS_KEY))
        {
            await CreateConfigurationAsync(
                MAX_MEAL_PLANS_KEY,
                DEFAULT_MAX_MEAL_PLANS.ToString(),
                "Maximum number of meal plans each customer can have",
                "System");
        }

        if (!existingConfigs.Any(c => c.ConfigKey == MAX_FRIDGE_ITEMS_KEY))
        {
            await CreateConfigurationAsync(
                MAX_FRIDGE_ITEMS_KEY,
                DEFAULT_MAX_FRIDGE_ITEMS.ToString(),
                "Maximum number of items each customer can have in their fridge",
                "System");
        }

        _logger.LogInformation("Default system configurations initialized");
    }

    private async Task<SystemConfiguration?> GetConfigurationAsync(string key)
    {
        var configs = await _unitOfWork.SystemConfigurations.FindAsync(c => c.ConfigKey == key);
        return configs.FirstOrDefault();
    }

    private async Task CreateConfigurationAsync(string key, string value, string description, string updatedBy)
    {
        var config = new SystemConfiguration
        {
            Id = Guid.NewGuid(),
            ConfigKey = key,
            ConfigValue = value,
            Description = description,
            UpdatedBy = updatedBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.SystemConfigurations.AddAsync(config);
        await _unitOfWork.SaveChangesAsync();
    }

    private async Task UpdateConfigurationAsync(string key, string value, string description, string updatedBy)
    {
        var config = await GetConfigurationAsync(key);
        
        if (config == null)
        {
            await CreateConfigurationAsync(key, value, description, updatedBy);
        }
        else
        {
            config.ConfigValue = value;
            config.Description = description;
            config.UpdatedBy = updatedBy;
            config.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SystemConfigurations.UpdateAsync(config);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
