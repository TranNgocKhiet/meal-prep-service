using System.ComponentModel.DataAnnotations;

namespace MealPrepService.Web.PresentationLayer.ViewModels
{
    public class HealthProfileViewModel
    {
        public Guid Id { get; set; }
        public Guid AccountId { get; set; }

        [Required(ErrorMessage = "Age is required")]
        [Range(1, 150, ErrorMessage = "Age must be between 1 and 150")]
        [Display(Name = "Age")]
        public int Age { get; set; }

        [Required(ErrorMessage = "Weight is required")]
        [Range(0.1, 1000, ErrorMessage = "Weight must be between 0.1 and 1000")]
        [Display(Name = "Weight (kg)")]
        public float Weight { get; set; }

        [Required(ErrorMessage = "Height is required")]
        [Range(0.1, 300, ErrorMessage = "Height must be between 0.1 and 300")]
        [Display(Name = "Height (cm)")]
        public float Height { get; set; }

        [Required(ErrorMessage = "Gender is required")]
        [Display(Name = "Gender")]
        public string Gender { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Health notes cannot exceed 1000 characters")]
        [Display(Name = "Health Notes")]
        [DataType(DataType.MultilineText)]
        public string HealthNotes { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Dietary restrictions cannot exceed 500 characters")]
        [Display(Name = "Dietary Restrictions")]
        [DataType(DataType.MultilineText)]
        public string? DietaryRestrictions { get; set; }

        [StringLength(500, ErrorMessage = "Food preferences cannot exceed 500 characters")]
        [Display(Name = "Food Preferences")]
        [DataType(DataType.MultilineText)]
        public string? FoodPreferences { get; set; }

        [Range(800, 5000, ErrorMessage = "Calorie goal must be between 800 and 5000")]
        [Display(Name = "Daily Calorie Goal")]
        public int? CalorieGoal { get; set; }

        [Display(Name = "Selected Allergies")]
        public List<Guid> SelectedAllergyIds { get; set; } = new List<Guid>();

        // For display purposes
        public List<AllergyViewModel> AvailableAllergies { get; set; } = new List<AllergyViewModel>();
        public List<string> CurrentAllergies { get; set; } = new List<string>();

        // Gender options for dropdown
        public static List<string> GenderOptions => new List<string> { "Male", "Female", "Other" };
    }

    public class AllergyViewModel
    {
        public Guid Id { get; set; }
        public string AllergyName { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
    }
}