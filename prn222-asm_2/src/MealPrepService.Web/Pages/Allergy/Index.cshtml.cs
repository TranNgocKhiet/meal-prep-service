using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;


namespace MealPrepService.Web.Pages.Allergy;

[Authorize(Roles = "Manager,Admin")]
public class IndexModel : PageModel
{
    private readonly IAllergyService _allergyService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IAllergyService allergyService, ILogger<IndexModel> logger)
    {
        _allergyService = allergyService;
        _logger = logger;
    }

    public List<AllergyDto> Allergies { get; set; } = new();
    public string SearchTerm { get; set; } = string.Empty;
    public bool ShowAll { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }

    // Helper properties for UI
    public bool HasAllergies => Allergies.Any();
    public int TotalAllergies => TotalItems;
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;

    public async Task<IActionResult> OnGetAsync(string searchTerm = "", bool showAll = false, int pageNumber = 1)
    {
        try
        {
            const int pageSize = 30;
            
            // Always load allergies
            var allergies = await _allergyService.GetAllAsync();
            var allergyList = allergies.ToList();
            
            // Apply search filter if search term is provided
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                allergyList = allergyList
                    .Where(a => a.AllergyName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var totalItems = allergyList.Count;
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            pageNumber = Math.Max(1, Math.Min(pageNumber, Math.Max(1, totalPages)));
            
            Allergies = allergyList
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            SearchTerm = searchTerm;
            ShowAll = showAll;
            CurrentPage = pageNumber;
            TotalPages = totalPages;
            PageSize = pageSize;
            TotalItems = totalItems;

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving allergies");
            TempData["ErrorMessage"] = "An error occurred while loading allergies.";
            return Page();
        }
    }
}
