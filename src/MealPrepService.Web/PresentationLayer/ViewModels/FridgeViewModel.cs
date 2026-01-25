using System.ComponentModel.DataAnnotations;

namespace MealPrepService.Web.PresentationLayer.ViewModels
{
    public class FridgeViewModel
    {
        public List<FridgeItemViewModel> FridgeItems { get; set; } = new List<FridgeItemViewModel>();
        public List<FridgeItemViewModel> ExpiringItems { get; set; } = new List<FridgeItemViewModel>();
        public List<FridgeItemViewModel> ExpiredItems { get; set; } = new List<FridgeItemViewModel>();
        
        // Summary statistics
        public int TotalItems { get; set; }
        public int ExpiringItemsCount { get; set; }
        public int ExpiredItemsCount { get; set; }
    }

    public class FridgeItemViewModel
    {
        public Guid Id { get; set; }
        public Guid AccountId { get; set; }
        public Guid IngredientId { get; set; }

        [Display(Name = "Ingredient")]
        public string IngredientName { get; set; } = string.Empty;

        [Display(Name = "Unit")]
        public string Unit { get; set; } = string.Empty;

        [Display(Name = "Current Amount")]
        public float CurrentAmount { get; set; }

        [Display(Name = "Expiry Date")]
        [DataType(DataType.Date)]
        public DateTime ExpiryDate { get; set; }

        [Display(Name = "Expiring Soon")]
        public bool IsExpiring { get; set; }

        [Display(Name = "Expired")]
        public bool IsExpired { get; set; }

        // Calculated properties for display
        public int DaysUntilExpiry => (ExpiryDate.Date - DateTime.Today).Days;
        public string ExpiryStatus => IsExpired ? "Expired" : IsExpiring ? "Expiring Soon" : "Fresh";
        public string ExpiryStatusClass => IsExpired ? "text-danger" : IsExpiring ? "text-warning" : "text-success";
    }

    public class AddFridgeItemViewModel
    {
        [Required(ErrorMessage = "Please select an ingredient")]
        [Display(Name = "Ingredient")]
        public Guid IngredientId { get; set; }

        [Required(ErrorMessage = "Amount is required")]
        [Range(0.01, 10000, ErrorMessage = "Amount must be between 0.01 and 10000")]
        [Display(Name = "Amount")]
        public float CurrentAmount { get; set; }

        [Required(ErrorMessage = "Expiry date is required")]
        [Display(Name = "Expiry Date")]
        [DataType(DataType.Date)]
        public DateTime ExpiryDate { get; set; } = DateTime.Today.AddDays(7);

        // For display purposes
        public List<IngredientSelectionViewModel> AvailableIngredients { get; set; } = new List<IngredientSelectionViewModel>();

        // Validation method
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (ExpiryDate < DateTime.Today)
            {
                yield return new ValidationResult("Expiry date cannot be in the past", new[] { nameof(ExpiryDate) });
            }

            if (ExpiryDate > DateTime.Today.AddYears(5))
            {
                yield return new ValidationResult("Expiry date cannot be more than 5 years in the future", new[] { nameof(ExpiryDate) });
            }
        }
    }

    public class IngredientSelectionViewModel
    {
        public Guid Id { get; set; }
        public string IngredientName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
    }

    public class UpdateQuantityViewModel
    {
        public Guid FridgeItemId { get; set; }

        [Display(Name = "Ingredient")]
        public string IngredientName { get; set; } = string.Empty;

        [Display(Name = "Current Amount")]
        public float CurrentAmount { get; set; }

        [Required(ErrorMessage = "New amount is required")]
        [Range(0, 10000, ErrorMessage = "Amount must be between 0 and 10000")]
        [Display(Name = "New Amount")]
        public float NewAmount { get; set; }

        [Display(Name = "Unit")]
        public string Unit { get; set; } = string.Empty;
    }

    public class GroceryListViewModel
    {
        public Guid AccountId { get; set; }
        public Guid MealPlanId { get; set; }

        [Display(Name = "Meal Plan")]
        public string MealPlanName { get; set; } = string.Empty;

        [Display(Name = "Generated Date")]
        [DataType(DataType.DateTime)]
        public DateTime GeneratedDate { get; set; }

        public List<GroceryItemViewModel> MissingIngredients { get; set; } = new List<GroceryItemViewModel>();

        // Summary statistics
        public int TotalMissingItems => MissingIngredients.Count;
        public decimal EstimatedCost { get; set; } // Could be calculated if ingredient prices are available
    }

    public class GroceryItemViewModel
    {
        public Guid IngredientId { get; set; }

        [Display(Name = "Ingredient")]
        public string IngredientName { get; set; } = string.Empty;

        [Display(Name = "Unit")]
        public string Unit { get; set; } = string.Empty;

        [Display(Name = "Required Amount")]
        public float RequiredAmount { get; set; }

        [Display(Name = "Current Amount")]
        public float CurrentAmount { get; set; }

        [Display(Name = "Needed Amount")]
        public float NeededAmount { get; set; }

        [Display(Name = "Needed Soon")]
        public bool IsNeededSoon { get; set; }

        [Display(Name = "Needed By")]
        public DateTime? EarliestNeededDate { get; set; }
        
        // Editable properties for purchase
        [Display(Name = "Purchase Quantity")]
        [Range(0.01, 10000, ErrorMessage = "Amount must be between 0.01 and 10000")]
        public float PurchaseAmount { get; set; }
        
        [Display(Name = "Expiry Date")]
        [DataType(DataType.Date)]
        public DateTime PurchaseExpiryDate { get; set; } = DateTime.Today.AddDays(7);

        // Display properties
        public string RequiredAmountDisplay => $"{RequiredAmount:F2} {Unit}";
        public string CurrentAmountDisplay => $"{CurrentAmount:F2} {Unit}";
        public string NeededAmountDisplay => $"{NeededAmount:F2} {Unit}";
        public string NeededByDisplay => EarliestNeededDate.HasValue 
            ? EarliestNeededDate.Value.ToString("MMM dd") 
            : "Unknown";
        public string PriorityBadge => IsNeededSoon ? "badge bg-danger" : "badge bg-secondary";
        public string PriorityText => IsNeededSoon ? "?? Urgent" : "Later";
    }

    public class GenerateGroceryListViewModel
    {
        [Required(ErrorMessage = "Please select a meal plan")]
        [Display(Name = "Meal Plan")]
        public Guid MealPlanId { get; set; }

        // For display purposes
        public List<MealPlanSelectionViewModel> AvailableMealPlans { get; set; } = new List<MealPlanSelectionViewModel>();
    }

    public class MealPlanSelectionViewModel
    {
        public Guid Id { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsAiGenerated { get; set; }
        public bool IsActive { get; set; }
        public bool IsSelected { get; set; }

        // Display properties
        public string DateRangeDisplay => $"{StartDate:MMM dd} - {EndDate:MMM dd, yyyy}";
        public string PlanTypeDisplay => IsAiGenerated ? "AI Generated" : "Manual";
    }

    public class FridgeStatsViewModel
    {
        public int TotalItems { get; set; }
        public int FreshItems { get; set; }
        public int ExpiringItems { get; set; }
        public int ExpiredItems { get; set; }
        public DateTime LastUpdated { get; set; }

        // Calculated properties
        public double FreshPercentage => TotalItems > 0 ? (double)FreshItems / TotalItems * 100 : 0;
        public double ExpiringPercentage => TotalItems > 0 ? (double)ExpiringItems / TotalItems * 100 : 0;
        public double ExpiredPercentage => TotalItems > 0 ? (double)ExpiredItems / TotalItems * 100 : 0;
    }

    public class SyncFridgeViewModel
    {
        public Guid MealPlanId { get; set; }
        public Dictionary<Guid, PurchasedIngredientViewModel> PurchasedIngredients { get; set; } = new Dictionary<Guid, PurchasedIngredientViewModel>();
    }

    public class PurchasedIngredientViewModel
    {
        public Guid IngredientId { get; set; }
        
        [Required]
        [Range(0.01, 10000, ErrorMessage = "Amount must be between 0.01 and 10000")]
        public float Amount { get; set; }
        
        public bool IsPurchased { get; set; }
        
        [Required]
        [DataType(DataType.Date)]
        public DateTime ExpiryDate { get; set; } = DateTime.Today.AddDays(7);
        
        public string Unit { get; set; } = string.Empty;
    }
}