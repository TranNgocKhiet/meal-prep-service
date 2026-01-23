using System.ComponentModel.DataAnnotations;

namespace MealPrepService.Web.PresentationLayer.ViewModels
{
    public class MealPlanViewModel
    {
        public Guid Id { get; set; }
        public Guid AccountId { get; set; }

        [Display(Name = "Plan Name")]
        public string PlanName { get; set; } = string.Empty;

        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Display(Name = "AI Generated")]
        public bool IsAiGenerated { get; set; }

        [Display(Name = "Active Plan")]
        public bool IsActive { get; set; }

        public List<MealViewModel> Meals { get; set; } = new List<MealViewModel>();

        // Calculated nutrition totals for the entire plan
        [Display(Name = "Total Calories")]
        public float TotalCalories { get; set; }

        [Display(Name = "Total Protein (g)")]
        public float TotalProteinG { get; set; }

        [Display(Name = "Total Fat (g)")]
        public float TotalFatG { get; set; }

        [Display(Name = "Total Carbs (g)")]
        public float TotalCarbsG { get; set; }

        // Daily nutrition breakdown
        public Dictionary<DateTime, DailyNutritionViewModel> DailyNutrition { get; set; } = new Dictionary<DateTime, DailyNutritionViewModel>();
    }

    public class CreateMealPlanViewModel
    {
        [Required(ErrorMessage = "Plan name is required")]
        [StringLength(100, ErrorMessage = "Plan name cannot exceed 100 characters")]
        [Display(Name = "Plan Name")]
        public string PlanName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Start date is required")]
        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "End date is required")]
        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; } = DateTime.Today.AddDays(7);

        // For AI generation
        [Display(Name = "Generate with AI")]
        public bool GenerateWithAI { get; set; }

        // Validation method
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (EndDate <= StartDate)
            {
                yield return new ValidationResult("End date must be after start date", new[] { nameof(EndDate) });
            }

            var daysDifference = (EndDate - StartDate).Days;
            if (daysDifference > 30)
            {
                yield return new ValidationResult("Meal plan cannot exceed 30 days", new[] { nameof(EndDate) });
            }
        }
    }

    public class MealViewModel
    {
        public Guid Id { get; set; }
        public Guid PlanId { get; set; }

        [Display(Name = "Meal Type")]
        public string MealType { get; set; } = string.Empty;

        [Display(Name = "Serve Date")]
        [DataType(DataType.Date)]
        public DateTime ServeDate { get; set; }

        public List<RecipeViewModel> Recipes { get; set; } = new List<RecipeViewModel>();

        // Calculated nutrition totals for this meal
        [Display(Name = "Meal Calories")]
        public float MealCalories { get; set; }

        [Display(Name = "Meal Protein (g)")]
        public float MealProteinG { get; set; }

        [Display(Name = "Meal Fat (g)")]
        public float MealFatG { get; set; }

        [Display(Name = "Meal Carbs (g)")]
        public float MealCarbsG { get; set; }

        // Meal type options
        public static List<string> MealTypeOptions => new List<string> { "Breakfast", "Lunch", "Dinner" };
        
        // Helper method to get meal type order for sorting
        public static int GetMealTypeOrder(string mealType)
        {
            return mealType switch
            {
                "Breakfast" => 1,
                "Lunch" => 2,
                "Dinner" => 3,
                _ => 4
            };
        }
    }

    public class RecipeViewModel
    {
        public Guid Id { get; set; }

        [Display(Name = "Recipe Name")]
        public string RecipeName { get; set; } = string.Empty;

        [Display(Name = "Instructions")]
        [DataType(DataType.MultilineText)]
        public string Instructions { get; set; } = string.Empty;

        // Nutrition information from existing recipe data
        [Display(Name = "Calories")]
        public float TotalCalories { get; set; }

        [Display(Name = "Protein (g)")]
        public float ProteinG { get; set; }

        [Display(Name = "Fat (g)")]
        public float FatG { get; set; }

        [Display(Name = "Carbs (g)")]
        public float CarbsG { get; set; }

        // For display purposes
        public List<RecipeIngredientViewModel> Ingredients { get; set; } = new List<RecipeIngredientViewModel>();
    }

    public class RecipeIngredientViewModel
    {
        public Guid IngredientId { get; set; }
        public string IngredientName { get; set; } = string.Empty;
        public float Amount { get; set; }
        public string Unit { get; set; } = string.Empty;
    }

    public class DailyNutritionViewModel
    {
        public DateTime Date { get; set; }
        public float DailyCalories { get; set; }
        public float DailyProteinG { get; set; }
        public float DailyFatG { get; set; }
        public float DailyCarbsG { get; set; }
        public List<MealViewModel> Meals { get; set; } = new List<MealViewModel>();
    }

    public class AddMealToPlanViewModel
    {
        public Guid PlanId { get; set; }

        [Required(ErrorMessage = "Meal type is required")]
        [Display(Name = "Meal Type")]
        public string MealType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Serve date is required")]
        [Display(Name = "Serve Date")]
        [DataType(DataType.Date)]
        public DateTime ServeDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "At least one recipe must be selected")]
        [Display(Name = "Selected Recipes")]
        public List<Guid> SelectedRecipeIds { get; set; } = new List<Guid>();

        // For display purposes
        public List<RecipeSelectionViewModel> AvailableRecipes { get; set; } = new List<RecipeSelectionViewModel>();
        
        public static List<string> MealTypeOptions => MealViewModel.MealTypeOptions;
    }

    public class RecipeSelectionViewModel
    {
        public Guid Id { get; set; }
        public string RecipeName { get; set; } = string.Empty;
        public float TotalCalories { get; set; }
        public float ProteinG { get; set; }
        public float FatG { get; set; }
        public float CarbsG { get; set; }
        public bool IsSelected { get; set; }
        public List<RecipeIngredientViewModel> Ingredients { get; set; } = new List<RecipeIngredientViewModel>();
    }

    public class RecipeIngredientCustomizationViewModel
    {
        public Guid RecipeId { get; set; }
        public Guid IngredientId { get; set; }
        public string IngredientName { get; set; } = string.Empty;
        public float OriginalAmount { get; set; }
        public float CustomAmount { get; set; }
        public string Unit { get; set; } = string.Empty;
    }
}