using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;


namespace MealPrepService.Web.Pages.Recipe;

[Authorize(Roles = "Admin,Manager")]
public class IndexModel : PageModel
{
    private readonly IRecipeService _recipeService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IRecipeService recipeService, ILogger<IndexModel> logger)
    {
        _recipeService = recipeService;
        _logger = logger;
    }

    public List<RecipeDto> Recipes { get; set; } = new();
    public string SearchTerm { get; set; }
    public bool ShowOnlyWithIngredients { get; set; }
    public bool ShowAll { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }

    // Helper properties for UI
    public int TotalRecipes => TotalItems;
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
    public bool HasRecipes => Recipes.Any();

    public async Task<IActionResult> OnGetAsync(string searchTerm = "", bool showOnlyWithIngredients = false, bool showAll = false, int pageNumber = 1)
    {
        try
        {
            const int pageSize = 30;
            var recipes = new List<RecipeDto>();

            // Always load recipes (either all or filtered)
            var recipeDtos = await _recipeService.GetAllAsync();
            recipes = recipeDtos.ToList();

            // Apply search filter if search term is provided
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                recipes = recipes.Where(r => 
                    r.RecipeName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    r.Instructions.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (showOnlyWithIngredients)
            {
                recipes = recipes.Where(r => r.Ingredients != null && r.Ingredients.Any()).ToList();
            }

            // Calculate pagination
            var totalItems = recipes.Count;
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            pageNumber = Math.Max(1, Math.Min(pageNumber, Math.Max(1, totalPages)));
            
            Recipes = recipes
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            SearchTerm = searchTerm;
            ShowOnlyWithIngredients = showOnlyWithIngredients;
            ShowAll = showAll;
            CurrentPage = pageNumber;
            TotalPages = totalPages;
            PageSize = pageSize;
            TotalItems = totalItems;

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving recipes");
            TempData["ErrorMessage"] = "An error occurred while loading the recipes.";
            Recipes = new List<RecipeDto>();
            return Page();
        }
    }
}
