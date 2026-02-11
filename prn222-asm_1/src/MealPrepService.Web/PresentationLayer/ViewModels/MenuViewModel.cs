using System.ComponentModel.DataAnnotations;

namespace MealPrepService.Web.PresentationLayer.ViewModels
{
    public class DailyMenuViewModel
    {
        public Guid Id { get; set; }

        [Display(Name = "Menu Date")]
        [DataType(DataType.Date)]
        public DateTime MenuDate { get; set; }

        [Display(Name = "Status")]
        public string Status { get; set; } = string.Empty; // draft, active

        [Display(Name = "Menu Meals")]
        public List<MenuMealViewModel> MenuMeals { get; set; } = new List<MenuMealViewModel>();

        // Calculated properties
        [Display(Name = "Total Meals")]
        public int TotalMeals => MenuMeals.Count;

        [Display(Name = "Available Meals")]
        public int AvailableMeals => MenuMeals.Count(m => !m.IsSoldOut);

        [Display(Name = "Sold Out Meals")]
        public int SoldOutMeals => MenuMeals.Count(m => m.IsSoldOut);

        // Status display properties
        public bool IsDraft => Status.Equals("draft", StringComparison.OrdinalIgnoreCase);
        public bool IsActive => Status.Equals("active", StringComparison.OrdinalIgnoreCase);
        public bool CanBePublished => IsDraft && MenuMeals.Any();
        public bool CanAddMeals => IsDraft;
    }

    public class MenuMealViewModel
    {
        public Guid Id { get; set; }
        public Guid MenuId { get; set; }
        public Guid RecipeId { get; set; }

        [Display(Name = "Recipe Name")]
        public string RecipeName { get; set; } = string.Empty;

        [Display(Name = "Price")]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        [Display(Name = "Available Quantity")]
        public int AvailableQuantity { get; set; }

        [Display(Name = "Sold Out")]
        public bool IsSoldOut { get; set; }

        // Recipe details for display
        public RecipeDetailsViewModel Recipe { get; set; } = new RecipeDetailsViewModel();

        // Status display properties
        public string AvailabilityStatus => IsSoldOut ? "Sold Out" : $"{AvailableQuantity} available";
        public string StatusCssClass => IsSoldOut ? "text-danger" : (AvailableQuantity < 5 ? "text-warning" : "text-success");
    }

    public class CreateMenuViewModel
    {
        [Required(ErrorMessage = "Menu date is required")]
        [Display(Name = "Menu Date")]
        [DataType(DataType.Date)]
        public DateTime MenuDate { get; set; } = DateTime.Today;

        // Validation method
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (MenuDate < DateTime.Today)
            {
                yield return new ValidationResult("Menu date cannot be in the past", new[] { nameof(MenuDate) });
            }

            // Check if menu already exists for this date (would be handled in controller/service)
        }
    }

    public class AddMealToMenuViewModel
    {
        public Guid MenuId { get; set; }

        [Required(ErrorMessage = "Recipe is required")]
        [Display(Name = "Recipe")]
        public Guid RecipeId { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, 999.99, ErrorMessage = "Price must be between $0.01 and $999.99")]
        [Display(Name = "Price")]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Available quantity is required")]
        [Range(1, 1000, ErrorMessage = "Available quantity must be between 1 and 1000")]
        [Display(Name = "Available Quantity")]
        public int AvailableQuantity { get; set; } = 10;

        // For display purposes
        public List<MenuRecipeSelectionViewModel> AvailableRecipes { get; set; } = new List<MenuRecipeSelectionViewModel>();
        public string MenuDateDisplay { get; set; } = string.Empty;
    }

    public class UpdateMenuQuantityViewModel
    {
        public Guid MenuMealId { get; set; }

        [Required(ErrorMessage = "Quantity is required")]
        [Range(0, 1000, ErrorMessage = "Quantity must be between 0 and 1000")]
        [Display(Name = "New Quantity")]
        public int NewQuantity { get; set; }

        // For display purposes
        public string RecipeName { get; set; } = string.Empty;
        public int CurrentQuantity { get; set; }
    }

    public class RecipeDetailsViewModel
    {
        public Guid Id { get; set; }

        [Display(Name = "Recipe Name")]
        public string RecipeName { get; set; } = string.Empty;

        [Display(Name = "Instructions")]
        [DataType(DataType.MultilineText)]
        public string Instructions { get; set; } = string.Empty;

        // Nutrition information
        [Display(Name = "Calories")]
        public float TotalCalories { get; set; }

        [Display(Name = "Protein (g)")]
        public float ProteinG { get; set; }

        [Display(Name = "Fat (g)")]
        public float FatG { get; set; }

        [Display(Name = "Carbs (g)")]
        public float CarbsG { get; set; }

        // Formatted nutrition display
        public string NutritionSummary => $"{TotalCalories:F0} cal | {ProteinG:F1}g protein | {FatG:F1}g fat | {CarbsG:F1}g carbs";
    }

    public class MenuRecipeSelectionViewModel
    {
        public Guid Id { get; set; }
        public string RecipeName { get; set; } = string.Empty;
        public float TotalCalories { get; set; }
        public float ProteinG { get; set; }
        public float FatG { get; set; }
        public float CarbsG { get; set; }
        public bool IsSelected { get; set; }
        public string NutritionSummary => $"{TotalCalories:F0} cal | {ProteinG:F1}g protein | {FatG:F1}g fat | {CarbsG:F1}g carbs";
    }

    // ViewModels for public menu viewing (Guest/Customer)
    public class PublicMenuViewModel
    {
        [Display(Name = "Menu Date")]
        [DataType(DataType.Date)]
        public DateTime MenuDate { get; set; }

        [Display(Name = "Available Meals")]
        public List<PublicMenuMealViewModel> AvailableMeals { get; set; } = new List<PublicMenuMealViewModel>();

        public bool HasMeals => AvailableMeals.Any();
        public string MenuDateDisplay => MenuDate.ToString("dddd, MMMM dd, yyyy");
    }

    public class PublicMenuMealViewModel
    {
        public Guid Id { get; set; }
        public Guid RecipeId { get; set; }

        [Display(Name = "Recipe Name")]
        public string RecipeName { get; set; } = string.Empty;

        [Display(Name = "Price")]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        [Display(Name = "Available Quantity")]
        public int AvailableQuantity { get; set; }

        [Display(Name = "Sold Out")]
        public bool IsSoldOut { get; set; }

        // Recipe details
        public RecipeDetailsViewModel Recipe { get; set; } = new RecipeDetailsViewModel();

        // Display properties
        public string AvailabilityStatus => IsSoldOut ? "Sold Out" : $"{AvailableQuantity} available";
        public string StatusCssClass => IsSoldOut ? "text-danger" : (AvailableQuantity < 5 ? "text-warning" : "text-success");
        public bool CanOrder => !IsSoldOut && AvailableQuantity > 0;
    }

    public class WeeklyMenuViewModel
    {
        [Display(Name = "Week Starting")]
        [DataType(DataType.Date)]
        public DateTime WeekStartDate { get; set; }

        [Display(Name = "Week Ending")]
        [DataType(DataType.Date)]
        public DateTime WeekEndDate { get; set; }

        [Display(Name = "Daily Menus")]
        public List<PublicMenuViewModel> DailyMenus { get; set; } = new List<PublicMenuViewModel>();

        public bool HasMenus => DailyMenus.Any(m => m.HasMeals);
        public string WeekDisplay => $"{WeekStartDate:MMM dd} - {WeekEndDate:MMM dd, yyyy}";

        // Navigation properties
        public DateTime PreviousWeekStart => WeekStartDate.AddDays(-7);
        public DateTime NextWeekStart => WeekStartDate.AddDays(7);
    }
}