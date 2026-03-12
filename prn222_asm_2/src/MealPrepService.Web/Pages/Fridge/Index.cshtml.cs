using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;


namespace MealPrepService.Web.Pages.Fridge;

[Authorize(Roles = "Customer")]
public class IndexModel : PageModel
{
    private readonly IFridgeService _fridgeService;
    private readonly IIngredientService _ingredientService;
    private readonly IMealPlanService _mealPlanService;
    private readonly ILogger<IndexModel> _logger;

    public List<FridgeItemDto> FridgeItems { get; set; } = new();
    public List<FridgeItemDto> ExpiringItems { get; set; } = new();
    public List<FridgeItemDto> ExpiredItems { get; set; } = new();
    public int TotalItems { get; set; }
    public int ExpiringItemsCount { get; set; }
    public int ExpiredItemsCount { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public string SearchTerm { get; set; } = string.Empty;

    // Helper properties for pagination
    public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;

    public IndexModel(
        IFridgeService fridgeService, 
        IIngredientService ingredientService, 
        IMealPlanService mealPlanService,
        ILogger<IndexModel> logger)
    {
        _fridgeService = fridgeService ?? throw new ArgumentNullException(nameof(fridgeService));
        _ingredientService = ingredientService ?? throw new ArgumentNullException(nameof(ingredientService));
        _mealPlanService = mealPlanService ?? throw new ArgumentNullException(nameof(mealPlanService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Initialize properties to prevent null reference errors
        FridgeItems = new List<FridgeItemDto>();
        ExpiringItems = new List<FridgeItemDto>();
        ExpiredItems = new List<FridgeItemDto>();
        TotalItems = 0;
        ExpiringItemsCount = 0;
        ExpiredItemsCount = 0;
        CurrentPage = 1;
        PageSize = 20;
        SearchTerm = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync(int pageNumber = 1, string searchTerm = "", long? t = null)
    {
        // Prevent browser caching
        Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
        Response.Headers["Pragma"] = "no-cache";
        Response.Headers["Expires"] = "0";
        
        // The 't' parameter is just a cache buster, we don't use it
        
        try
        {
            var accountId = GetCurrentAccountId();
            const int pageSize = 20;
            
            if (pageNumber < 1)
            {
                pageNumber = 1;
            }
            
            SearchTerm = searchTerm?.Trim() ?? string.Empty;
            
            // Get all fridge items for the account
            var allFridgeItems = await _fridgeService.GetFridgeItemsAsync(accountId);
            var fridgeItemsList = allFridgeItems.ToList();
            
            // Apply search filter if search term is provided
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                fridgeItemsList = fridgeItemsList
                    .Where(item => item.IngredientName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            
            // Get total count after filtering
            var totalCount = fridgeItemsList.Count;
            
            // Apply pagination
            var pagedFridgeItems = fridgeItemsList
                .OrderBy(item => item.ExpiryDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            
            var expiringItems = await _fridgeService.GetExpiringItemsAsync(accountId);
            
            FridgeItems = pagedFridgeItems;
            ExpiringItems = expiringItems.Where(item => item.IsExpiring && !item.IsExpired).ToList();
            ExpiredItems = expiringItems.Where(item => item.IsExpired).ToList();
            TotalItems = totalCount;
            ExpiringItemsCount = ExpiringItems.Count;
            ExpiredItemsCount = ExpiredItems.Count;
            CurrentPage = pageNumber;
            PageSize = pageSize;
            
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving fridge items for account {AccountId}", GetCurrentAccountId());
            TempData["ErrorMessage"] = "An error occurred while loading your fridge items.";
            
            // Initialize empty lists to prevent null reference errors
            FridgeItems = new List<FridgeItemDto>();
            ExpiringItems = new List<FridgeItemDto>();
            ExpiredItems = new List<FridgeItemDto>();
            TotalItems = 0;
            ExpiringItemsCount = 0;
            ExpiredItemsCount = 0;
            CurrentPage = 1;
            PageSize = 20;
            SearchTerm = searchTerm?.Trim() ?? string.Empty;
            
            return Page();
        }
    }

    private Guid GetCurrentAccountId()
    {
        var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(accountIdClaim) || !Guid.TryParse(accountIdClaim, out var accountId))
        {
            throw new AuthenticationException("User account ID not found in claims.");
        }
        return accountId;
    }
}
