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
    }

    public async Task<IActionResult> OnGetAsync(int pageNumber = 1)
    {
        try
        {
            var accountId = GetCurrentAccountId();
            const int pageSize = 20;
            
            if (pageNumber < 1)
            {
                pageNumber = 1;
            }
            
            var (pagedFridgeItems, totalCount) = await _fridgeService.GetFridgeItemsPagedAsync(accountId, pageNumber, pageSize);
            var expiringItems = await _fridgeService.GetExpiringItemsAsync(accountId);
            
            FridgeItems = pagedFridgeItems.ToList();
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
