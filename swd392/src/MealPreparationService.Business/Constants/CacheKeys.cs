namespace MealPreparationService.Business.Constants;

/// <summary>
/// Cache key constants for consistent cache key generation
/// </summary>
public static class CacheKeys
{
    // Recipe cache keys
    public const string RecipeById = "recipe:{0}";
    public const string RecipesByCategory = "recipes:category:{0}";
    public const string RecipeSearch = "recipes:search:{0}:{1}:{2}:{3}"; // searchTerm:category:maxPrepTime:difficulty
    public const string AllRecipes = "recipes:all";

    // Ingredient cache keys
    public const string IngredientById = "ingredient:{0}";
    public const string IngredientsByCategory = "ingredients:category:{0}";
    public const string IngredientSearch = "ingredients:search:{0}:{1}"; // searchTerm:category
    public const string AllIngredients = "ingredients:all";

    // Configuration cache keys
    public const string SystemConfiguration = "config:system";
    public const string ConfigurationByKey = "config:{0}";

    // Allergy cache keys
    public const string AllergiesByUser = "allergies:user:{0}";
    public const string AllAllergies = "allergies:all";

    // Nutrient cache keys
    public const string NutrientsByIngredient = "nutrients:ingredient:{0}";
    public const string AllNutrients = "nutrients:all";

    // Cache expiration times
    public static readonly TimeSpan RecipeCacheExpiration = TimeSpan.FromHours(24);
    public static readonly TimeSpan IngredientCacheExpiration = TimeSpan.FromHours(24);
    public static readonly TimeSpan ConfigurationCacheExpiration = TimeSpan.FromHours(12);
    public static readonly TimeSpan AllergyCacheExpiration = TimeSpan.FromHours(6);
    public static readonly TimeSpan NutrientCacheExpiration = TimeSpan.FromHours(24);

    /// <summary>
    /// Generates a cache key for recipe by ID
    /// </summary>
    public static string GetRecipeByIdKey(string recipeId) => string.Format(RecipeById, recipeId);

    /// <summary>
    /// Generates a cache key for recipes by category
    /// </summary>
    public static string GetRecipesByCategoryKey(string category) => string.Format(RecipesByCategory, category);

    /// <summary>
    /// Generates a cache key for recipe search
    /// </summary>
    public static string GetRecipeSearchKey(string? searchTerm, string? category, int? maxPrepTime, string? difficulty)
        => string.Format(RecipeSearch, searchTerm ?? "all", category ?? "all", maxPrepTime?.ToString() ?? "all", difficulty ?? "all");

    /// <summary>
    /// Generates a cache key for ingredient by ID
    /// </summary>
    public static string GetIngredientByIdKey(string ingredientId) => string.Format(IngredientById, ingredientId);

    /// <summary>
    /// Generates a cache key for ingredients by category
    /// </summary>
    public static string GetIngredientsByCategoryKey(string category) => string.Format(IngredientsByCategory, category);

    /// <summary>
    /// Generates a cache key for ingredient search
    /// </summary>
    public static string GetIngredientSearchKey(string? searchTerm, string? category)
        => string.Format(IngredientSearch, searchTerm ?? "all", category ?? "all");

    /// <summary>
    /// Generates a cache key for configuration by key
    /// </summary>
    public static string GetConfigurationKey(string key) => string.Format(ConfigurationByKey, key);

    /// <summary>
    /// Generates a cache key for user allergies
    /// </summary>
    public static string GetUserAllergiesKey(string userId) => string.Format(AllergiesByUser, userId);

    /// <summary>
    /// Generates a cache key for nutrients by ingredient
    /// </summary>
    public static string GetNutrientsByIngredientKey(string ingredientId) => string.Format(NutrientsByIngredient, ingredientId);
}
