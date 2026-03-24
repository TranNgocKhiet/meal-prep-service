using MealPreparationService.Business.DTOs;
using MealPreparationService.Domain.Services;

namespace MealPreparationService.Business.Services;

public class NutrientCalculatorService : INutrientCalculatorService
{
    private readonly IOpenAiService _openAiService;
    private readonly ICacheService _cacheService;
    private readonly IDateTimeService _dateTimeService;

    public NutrientCalculatorService(
        IOpenAiService openAiService,
        ICacheService cacheService,
        IDateTimeService dateTimeService)
    {
        _openAiService = openAiService;
        _cacheService = cacheService;
        _dateTimeService = dateTimeService;
    }

    public async Task<NutrientCalculationDto> CalculateNutrientsAsync(NutrientRequestDto dto, string userId)
    {
        if (dto.Ingredients == null || dto.Ingredients.Count == 0)
        {
            throw new InvalidOperationException("At least one ingredient is required");
        }

        var nutrientData = await _openAiService.CalculateNutrientsAsync(new NutrientPromptDto
        {
            Ingredients = dto.Ingredients
        });

        var adviceContext = $"Calories: {nutrientData.TotalCalories}, Protein: {nutrientData.TotalProteins}g, Carbs: {nutrientData.TotalCarbohydrates}g, Fats: {nutrientData.TotalFats}g";
        var healthAdvice = await _openAiService.GetHealthAdviceAsync(adviceContext);

        var calculation = new NutrientCalculationDto
        {
            Id = Guid.NewGuid().ToString(),
            CalculationName = dto.CalculationName,
            Ingredients = dto.Ingredients,
            NutrientData = nutrientData,
            HealthAdvice = healthAdvice,
            CalculatedAt = _dateTimeService.Now,
            IsSaved = false
        };

        await SaveCalculationToCacheAsync(userId, calculation);
        return calculation;
    }

    public async Task<NutrientCalculationDto?> GetSavedCalculationAsync(string calculationId, string userId)
    {
        var key = GetCalculationCacheKey(userId, calculationId);
        return await _cacheService.GetAsync<NutrientCalculationDto>(key);
    }

    public async Task<List<NutrientCalculationDto>> GetUserCalculationsAsync(string userId)
    {
        var history = await GetHistoryAsync(userId);
        var results = new List<NutrientCalculationDto>();

        foreach (var calculationId in history.CalculationIds)
        {
            var calculation = await GetSavedCalculationAsync(calculationId, userId);
            if (calculation != null)
            {
                results.Add(calculation);
            }
        }

        return results
            .OrderByDescending(c => c.CalculatedAt)
            .ToList();
    }

    public async Task SaveCalculationAsync(string userId, string calculationId)
    {
        var calculation = await GetSavedCalculationAsync(calculationId, userId);
        if (calculation == null)
        {
            throw new KeyNotFoundException("Calculation not found");
        }

        calculation.IsSaved = true;
        await _cacheService.SetAsync(GetCalculationCacheKey(userId, calculationId), calculation, TimeSpan.FromDays(30));
    }

    private async Task SaveCalculationToCacheAsync(string userId, NutrientCalculationDto calculation)
    {
        await _cacheService.SetAsync(GetCalculationCacheKey(userId, calculation.Id), calculation, TimeSpan.FromDays(7));

        var history = await GetHistoryAsync(userId);
        history.CalculationIds.RemoveAll(id => id == calculation.Id);
        history.CalculationIds.Insert(0, calculation.Id);

        if (history.CalculationIds.Count > 50)
        {
            history.CalculationIds = history.CalculationIds.Take(50).ToList();
        }

        await _cacheService.SetAsync(GetHistoryCacheKey(userId), history, TimeSpan.FromDays(30));
    }

    private async Task<NutrientCalculationHistoryCache> GetHistoryAsync(string userId)
    {
        var key = GetHistoryCacheKey(userId);
        var history = await _cacheService.GetAsync<NutrientCalculationHistoryCache>(key);
        return history ?? new NutrientCalculationHistoryCache();
    }

    private static string GetCalculationCacheKey(string userId, string calculationId) =>
        $"nutrient-calculation:{userId}:{calculationId}";

    private static string GetHistoryCacheKey(string userId) =>
        $"nutrient-calculation-history:{userId}";

    private class NutrientCalculationHistoryCache
    {
        public List<string> CalculationIds { get; set; } = new();
    }
}
