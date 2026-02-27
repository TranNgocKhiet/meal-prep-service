using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using MealPrepService.BusinessLogicLayer.Interfaces;
using System.Security.Claims;

namespace MealPrepService.Web.Pages.AITest;

[Authorize]
public class TestRecommendationsModel : PageModel
{
    private readonly IAIRecommendationService _aiRecommendationService;
    private readonly ILogger<TestRecommendationsModel> _logger;

    public TestRecommendationsModel(IAIRecommendationService aiRecommendationService, ILogger<TestRecommendationsModel> logger)
    {
        _aiRecommendationService = aiRecommendationService;
        _logger = logger;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            var allClaims = User.Claims.Select(c => $"{c.Type}: {c.Value}").ToList();
            _logger.LogInformation("User claims: {Claims}", string.Join(", ", allClaims));

            var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("NameIdentifier claim value: '{ClaimValue}'", accountIdClaim ?? "NULL");

            if (string.IsNullOrEmpty(accountIdClaim))
            {
                TempData["ErrorMessage"] = "User account ID claim not found. Please log out and log back in.";
                return RedirectToPage("/AITest/Index");
            }

            if (!Guid.TryParse(accountIdClaim, out var accountId))
            {
                TempData["ErrorMessage"] = $"Invalid account ID format: '{accountIdClaim}'. Expected GUID format.";
                _logger.LogError("Failed to parse account ID: '{AccountIdClaim}'", accountIdClaim);
                return RedirectToPage("/AITest/Index");
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

        return RedirectToPage("/AITest/Index");
    }
}
