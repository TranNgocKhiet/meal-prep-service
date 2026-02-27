using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.DataAccessLayer.Entities;

namespace MealPrepService.BusinessLogicLayer.Interfaces
{
    /// <summary>
    /// Interface for Large Language Model services (OpenAI, Azure OpenAI, Claude, etc.)
    /// </summary>
    public interface ILLMService
    {
        /// <summary>
        /// Generate meal recommendations using AI reasoning
        /// </summary>
        Task<AIRecommendationResponse> GenerateRecommendationsAsync(
            CustomerContext context, 
            List<Recipe> candidateRecipes,
            int maxRecommendations);

        /// <summary>
        /// Generate diverse meal recommendations that avoid recent recipes
        /// </summary>
        Task<AIRecommendationResponse> GenerateRecommendationsAsync(
            CustomerContext context,
            List<Recipe> candidateRecipes,
            int maxRecommendations,
            List<Guid> recentRecipeIds,
            DateTime targetDate,
            string mealType);

        /// <summary>
        /// Generate natural language explanation for a recommendation
        /// </summary>
        Task<string> GenerateRecommendationReasoningAsync(
            Recipe recipe,
            CustomerContext context);

        /// <summary>
        /// Check if the LLM service is available and healthy
        /// </summary>
        Task<bool> IsAvailableAsync();

        /// <summary>
        /// Get the current model being used
        /// </summary>
        string GetModelName();
    }

    public class AIRecommendationResponse
    {
        public List<AIRecommendedRecipe> RecommendedRecipes { get; set; } = new();
        public string OverallReasoning { get; set; } = string.Empty;
        public Dictionary<string, string> Metadata { get; set; } = new();
    }

    public class AIRecommendedRecipe
    {
        public Guid RecipeId { get; set; }
        public string RecipeName { get; set; } = string.Empty;
        public double ConfidenceScore { get; set; }
        public string Reasoning { get; set; } = string.Empty;
        public List<string> MatchedCriteria { get; set; } = new();
    }
}
