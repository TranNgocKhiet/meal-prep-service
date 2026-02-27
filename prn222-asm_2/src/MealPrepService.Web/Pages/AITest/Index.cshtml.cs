using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using MealPrepService.BusinessLogicLayer.Interfaces;

namespace MealPrepService.Web.Pages.AITest;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IAIRecommendationService _aiRecommendationService;
    private readonly ILLMService? _llmService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IAIRecommendationService aiRecommendationService,
        IConfiguration configuration,
        ILogger<IndexModel> logger,
        ILLMService? llmService = null)
    {
        _aiRecommendationService = aiRecommendationService;
        _configuration = configuration;
        _logger = logger;
        _llmService = llmService;
    }

    public bool IsAIEnabled { get; set; }
    public bool LLMServiceAvailable { get; set; }
    public bool LLMServiceHealthy { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public string ConfigurationStatus { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            var useRealAI = _configuration.GetValue<bool>("AI:UseRealAI", false);
            var apiKey = _configuration["AI:OpenAI:ApiKey"];
            var modelName = _configuration["AI:OpenAI:Model"];
            
            ConfigurationStatus = $"UseRealAI: {useRealAI}, ApiKey: {(string.IsNullOrEmpty(apiKey) ? "NOT SET" : $"SET ({apiKey.Length} chars)")}, Model: {modelName}";
            IsAIEnabled = await _aiRecommendationService.IsAIEnabledAsync();
            LLMServiceAvailable = _llmService != null;
            ModelName = _llmService?.GetModelName() ?? "N/A";

            if (_llmService != null)
            {
                try
                {
                    LLMServiceHealthy = await _llmService.IsAvailableAsync();
                }
                catch (Exception ex)
                {
                    LLMServiceHealthy = false;
                    ErrorMessage = ex.Message;
                    _logger.LogError(ex, "LLM service health check failed");
                }
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Configuration Error: {ex.Message}";
            _logger.LogError(ex, "Error in AI test controller");
        }

        return Page();
    }
}
