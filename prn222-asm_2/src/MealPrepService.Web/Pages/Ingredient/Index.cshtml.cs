using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;


namespace MealPrepService.Web.Pages.Ingredient;

[Authorize(Roles = "Admin,Manager")]
public class IndexModel : PageModel
{
    private readonly IIngredientService _ingredientService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IIngredientService ingredientService, ILogger<IndexModel> logger)
    {
        _ingredientService = ingredientService;
        _logger = logger;
    }

    public List<IngredientDto> Ingredients { get; set; } = new();
    public string SearchTerm { get; set; } = string.Empty;
    public bool? ShowOnlyAllergens { get; set; }
    public bool ShowAll { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }

    // Helper properties for UI
    public int TotalIngredients => TotalItems;
    public int AllergenCount => Ingredients.Count(i => i.IsAllergen);
    public int SafeIngredientCount => Ingredients.Count(i => !i.IsAllergen);
    public decimal AllergenPercentage => TotalItems > 0 ? (decimal)AllergenCount / TotalItems * 100 : 0;
    public bool HasIngredients => Ingredients.Any();
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;

    public async Task<IActionResult> OnGetAsync(string searchTerm = "", bool? showOnlyAllergens = null, bool showAll = false, int pageNumber = 1)
    {
        try
        {
            const int pageSize = 30;
            
            // Always load ingredients
            var ingredientDtos = await _ingredientService.GetAllAsync();
            var ingredientList = ingredientDtos.ToList();

            // Apply search filter if search term is provided
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                ingredientList = ingredientList.Where(i => 
                    i.IngredientName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    i.Unit.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (showOnlyAllergens.HasValue)
            {
                ingredientList = ingredientList.Where(i => i.IsAllergen == showOnlyAllergens.Value).ToList();
            }

            var totalItems = ingredientList.Count;
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            pageNumber = Math.Max(1, Math.Min(pageNumber, Math.Max(1, totalPages)));
            
            Ingredients = ingredientList
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            SearchTerm = searchTerm;
            ShowOnlyAllergens = showOnlyAllergens;
            ShowAll = showAll;
            CurrentPage = pageNumber;
            TotalPages = totalPages;
            PageSize = pageSize;
            TotalItems = totalItems;

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving ingredients");
            TempData["ErrorMessage"] = "An error occurred while loading the ingredients.";
            return Page();
        }
    }
}
