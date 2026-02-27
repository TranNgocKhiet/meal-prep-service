using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MealPrepService.BusinessLogicLayer.Interfaces;
using System.Security.Claims;

namespace MealPrepService.Web.PresentationLayer.Controllers
{
    /// <summary>
    /// Test controller for AI integration - Remove in production
    /// </summary>
    [Authorize]
    public class AITestController : Controller
    {
        private readonly IAIRecommendationService _aiRecommendationService;
        private readonly ILLMService? _llmService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AITestController> _logger;

        public AITestController(
            IAIRecommendationService aiRecommendationService,
            IConfiguration configuration,
            ILogger<AITestController> logger,
            ILLMService? llmService = null)
        {
            _aiRecommendationService = aiRecommendationService ?? throw new ArgumentNullException(nameof(aiRecommendationService));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _llmService = llmService;
        }

        // GET: AITest/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var model = new AITestViewModel();

            try
            {
                // Test configuration loading
                var useRealAI = _configuration.GetValue<bool>("AI:UseRealAI", false);
                var apiKey = _configuration["AI:OpenAI:ApiKey"];
                var modelName = _configuration["AI:OpenAI:Model"];
                
                model.ConfigurationStatus = $"UseRealAI: {useRealAI}, ApiKey: {(string.IsNullOrEmpty(apiKey) ? "NOT SET" : $"SET ({apiKey.Length} chars)")}, Model: {modelName}";
                model.IsAIEnabled = await _aiRecommendationService.IsAIEnabledAsync();
                model.LLMServiceAvailable = _llmService != null;
                model.ModelName = _llmService?.GetModelName() ?? "N/A";

                if (_llmService != null)
                {
                    try
                    {
                        model.LLMServiceHealthy = await _llmService.IsAvailableAsync();
                    }
                    catch (Exception ex)
                    {
                        model.LLMServiceHealthy = false;
                        model.ErrorMessage = ex.Message;
                        _logger.LogError(ex, "LLM service health check failed");
                    }
                }
            }
            catch (Exception ex)
            {
                model.ErrorMessage = $"Configuration Error: {ex.Message}";
                _logger.LogError(ex, "Error in AI test controller");
            }

            return View(model);
        }

        // POST: AITest/TestRecommendations
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TestRecommendations()
        {
            try
            {
                // Debug: Log all user claims
                var allClaims = User.Claims.Select(c => $"{c.Type}: {c.Value}").ToList();
                _logger.LogInformation("User claims: {Claims}", string.Join(", ", allClaims));

                var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("NameIdentifier claim value: '{ClaimValue}'", accountIdClaim ?? "NULL");

                if (string.IsNullOrEmpty(accountIdClaim))
                {
                    TempData["ErrorMessage"] = "User account ID claim not found. Please log out and log back in.";
                    return RedirectToAction(nameof(Index));
                }

                if (!Guid.TryParse(accountIdClaim, out var accountId))
                {
                    TempData["ErrorMessage"] = $"Invalid account ID format: '{accountIdClaim}'. Expected GUID format.";
                    _logger.LogError("Failed to parse account ID: '{AccountIdClaim}'", accountIdClaim);
                    return RedirectToAction(nameof(Index));
                }

                _logger.LogInformation("Testing AI recommendations for account: {AccountId}", accountId);
                var result = await _aiRecommendationService.GenerateRecommendationsAsync(accountId);

                if (result.Success)
                {
                    TempData["SuccessMessage"] = $"Generated {result.Recommendations.Count} recommendations successfully!";
                    TempData["RecommendationDetails"] = System.Text.Json.JsonSerializer.Serialize(result);
                }
                else
                {
                    TempData["ErrorMessage"] = result.ErrorMessage ?? "Failed to generate recommendations.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing AI recommendations");
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }

    public class AITestViewModel
    {
        public bool IsAIEnabled { get; set; }
        public bool LLMServiceAvailable { get; set; }
        public bool LLMServiceHealthy { get; set; }
        public string ModelName { get; set; } = string.Empty;
        public string ConfigurationStatus { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
    }
}
