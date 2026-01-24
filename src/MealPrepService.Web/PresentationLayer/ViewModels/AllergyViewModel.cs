using System.ComponentModel.DataAnnotations;

namespace MealPrepService.Web.PresentationLayer.ViewModels;

public class CreateAllergyViewModel
{
    [Required(ErrorMessage = "Allergy name is required")]
    [StringLength(100, ErrorMessage = "Allergy name cannot exceed 100 characters")]
    [Display(Name = "Allergy Name")]
    public string AllergyName { get; set; } = string.Empty;
}

public class EditAllergyViewModel
{
    public Guid Id { get; set; }
    
    [Required(ErrorMessage = "Allergy name is required")]
    [StringLength(100, ErrorMessage = "Allergy name cannot exceed 100 characters")]
    [Display(Name = "Allergy Name")]
    public string AllergyName { get; set; } = string.Empty;
}
