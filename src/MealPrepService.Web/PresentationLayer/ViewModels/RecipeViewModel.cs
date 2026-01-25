using System.ComponentModel.DataAnnotations;

namespace MealPrepService.Web.PresentationLayer.ViewModels
{
    // Note: RecipeViewModel already exists in MealPlanViewModel.cs and is reused here

    /// <summary>
    /// ViewModel for creating a new recipe
    /// </summary>
    public class CreateRecipeViewModel
    {
        [Required(ErrorMessage = "Recipe name is required")]
        [StringLength(200, ErrorMessage = "Recipe name cannot exceed 200 characters")]
        [Display(Name = "Recipe Name")]
        public string RecipeName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Instructions are required")]
        [StringLength(5000, ErrorMessage = "Instructions cannot exceed 5000 characters")]
        [Display(Name = "Instructions")]
        [DataType(DataType.MultilineText)]
        public string Instructions { get; set; } = string.Empty;
    }

    /// <summary>
    /// ViewModel for editing an existing recipe
    /// </summary>
    public class EditRecipeViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Recipe name is required")]
        [StringLength(200, ErrorMessage = "Recipe name cannot exceed 200 characters")]
        [Display(Name = "Recipe Name")]
        public string RecipeName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Instructions are required")]
        [StringLength(5000, ErrorMessage = "Instructions cannot exceed 5000 characters")]
        [Display(Name = "Instructions")]
        [DataType(DataType.MultilineText)]
        public string Instructions { get; set; } = string.Empty;

        // Current nutrition values (read-only, calculated from ingredients)
        [Display(Name = "Total Calories")]
        public float TotalCalories { get; set; }

        [Display(Name = "Protein (g)")]
        public float ProteinG { get; set; }

        [Display(Name = "Fat (g)")]
        public float FatG { get; set; }

        [Display(Name = "Carbs (g)")]
        public float CarbsG { get; set; }

        // Current ingredients
        [Display(Name = "Ingredients")]
        public List<RecipeIngredientViewModel> Ingredients { get; set; } = new List<RecipeIngredientViewModel>();

        // Formatted nutrition display
        public string NutritionSummary => $"{TotalCalories:F0} cal | {ProteinG:F1}g protein | {FatG:F1}g fat | {CarbsG:F1}g carbs";
    }

    /// <summary>
    /// ViewModel for adding an ingredient to a recipe
    /// </summary>
    public class AddIngredientToRecipeViewModel
    {
        public Guid RecipeId { get; set; }

        [Required(ErrorMessage = "Ingredient is required")]
        [Display(Name = "Ingredient")]
        public Guid IngredientId { get; set; }

        [Required(ErrorMessage = "Amount is required")]
        [Range(0.01, 10000, ErrorMessage = "Amount must be between 0.01 and 10000")]
        [Display(Name = "Amount")]
        public float Amount { get; set; }

        // For display purposes
        public string RecipeName { get; set; } = string.Empty;
        public List<RecipeIngredientSelectionViewModel> AvailableIngredients { get; set; } = new List<RecipeIngredientSelectionViewModel>();
    }

    /// <summary>
    /// ViewModel for ingredient selection in recipe management (extends base IngredientSelectionViewModel)
    /// </summary>
    public class RecipeIngredientSelectionViewModel
    {
        public Guid Id { get; set; }
        public string IngredientName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public float CaloPerUnit { get; set; }
        public bool IsAllergen { get; set; }
        public bool IsSelected { get; set; }

        public string DisplayText => IsAllergen 
            ? $"{IngredientName} ({Unit}) - {CaloPerUnit:F1} cal/unit [ALLERGEN]" 
            : $"{IngredientName} ({Unit}) - {CaloPerUnit:F1} cal/unit";
    }

    /// <summary>
    /// ViewModel for recipe list display
    /// </summary>
    public class RecipeListViewModel
    {
        public List<RecipeViewModel> Recipes { get; set; } = new List<RecipeViewModel>();

        public bool HasRecipes => Recipes.Any();
        public int TotalRecipes => TotalItems;

        // Filter properties
        public string SearchTerm { get; set; } = string.Empty;
        public bool ShowOnlyWithIngredients { get; set; }
        public bool ShowAll { get; set; }
        
        // Pagination properties
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int PageSize { get; set; } = 30;
        public int TotalItems { get; set; } = 0;
        
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }
}
