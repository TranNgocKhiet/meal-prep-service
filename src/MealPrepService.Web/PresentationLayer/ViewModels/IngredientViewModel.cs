using System.ComponentModel.DataAnnotations;

namespace MealPrepService.Web.PresentationLayer.ViewModels
{
    /// <summary>
    /// ViewModel for displaying ingredient details
    /// </summary>
    public class IngredientViewModel
    {
        public Guid Id { get; set; }

        [Display(Name = "Ingredient Name")]
        public string IngredientName { get; set; } = string.Empty;

        [Display(Name = "Unit")]
        public string Unit { get; set; } = string.Empty;

        [Display(Name = "Calories per Unit")]
        public float CaloPerUnit { get; set; }

        [Display(Name = "Is Allergen")]
        public bool IsAllergen { get; set; }

        public List<Guid> SelectedAllergyIds { get; set; } = new List<Guid>();
        public List<AllergyViewModel> Allergies { get; set; } = new List<AllergyViewModel>();

        // Display properties
        public string AllergenStatus => IsAllergen ? "Yes" : "No";
        public string AllergenBadge => IsAllergen ? "badge bg-danger" : "badge bg-success";
        public string AllergenText => IsAllergen ? "Allergen" : "Safe";
        public string AllergyName => IsAllergen && Allergies.Any() ? string.Join(", ", Allergies.Select(a => a.AllergyName)) : (IsAllergen ? IngredientName : "None");
        public string CaloriesDisplay => $"{CaloPerUnit:F1} cal/{Unit}";
    }

    /// <summary>
    /// ViewModel for creating a new ingredient
    /// </summary>
    public class CreateIngredientViewModel
    {
        [Required(ErrorMessage = "Ingredient name is required")]
        [StringLength(200, ErrorMessage = "Ingredient name cannot exceed 200 characters")]
        [Display(Name = "Ingredient Name")]
        public string IngredientName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Unit is required")]
        [StringLength(50, ErrorMessage = "Unit cannot exceed 50 characters")]
        [Display(Name = "Unit")]
        public string Unit { get; set; } = string.Empty;

        [Required(ErrorMessage = "Calories per unit is required")]
        [Range(0, 10000, ErrorMessage = "Calories per unit must be between 0 and 10000")]
        [Display(Name = "Calories per Unit")]
        public float CaloPerUnit { get; set; }

        [Display(Name = "Is Allergen")]
        public bool IsAllergen { get; set; }
    }

    /// <summary>
    /// ViewModel for editing an existing ingredient
    /// </summary>
    public class EditIngredientViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Ingredient name is required")]
        [StringLength(200, ErrorMessage = "Ingredient name cannot exceed 200 characters")]
        [Display(Name = "Ingredient Name")]
        public string IngredientName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Unit is required")]
        [StringLength(50, ErrorMessage = "Unit cannot exceed 50 characters")]
        [Display(Name = "Unit")]
        public string Unit { get; set; } = string.Empty;

        [Required(ErrorMessage = "Calories per unit is required")]
        [Range(0, 10000, ErrorMessage = "Calories per unit must be between 0 and 10000")]
        [Display(Name = "Calories per Unit")]
        public float CaloPerUnit { get; set; }

        [Display(Name = "Is Allergen")]
        public bool IsAllergen { get; set; }

        public List<Guid> SelectedAllergyIds { get; set; } = new List<Guid>();
        public List<AllergyViewModel> AvailableAllergies { get; set; } = new List<AllergyViewModel>();
    }

    /// <summary>
    /// ViewModel for ingredient list display
    /// </summary>
    public class IngredientListViewModel
    {
        public List<IngredientViewModel> Ingredients { get; set; } = new List<IngredientViewModel>();

        public bool HasIngredients => Ingredients.Any();
        public int TotalIngredients => Ingredients.Count;
        public int AllergenCount => Ingredients.Count(i => i.IsAllergen);
        public int SafeIngredientCount => Ingredients.Count(i => !i.IsAllergen);

        // Filter properties
        public string SearchTerm { get; set; } = string.Empty;
        public bool? ShowOnlyAllergens { get; set; }

        // Statistics
        public string AllergenPercentage => TotalIngredients > 0 
            ? $"{(AllergenCount * 100.0 / TotalIngredients):F1}%" 
            : "0%";
    }
}
