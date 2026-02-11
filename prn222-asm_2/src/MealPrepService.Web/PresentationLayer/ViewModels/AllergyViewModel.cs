using System.ComponentModel.DataAnnotations;

namespace MealPrepService.Web.PresentationLayer.ViewModels;

public class AllergyListViewModel
{
    public List<AllergyViewModel> Allergies { get; set; } = new List<AllergyViewModel>();
    public string SearchTerm { get; set; } = string.Empty;
    public bool ShowAll { get; set; }
    
    public bool HasAllergies => Allergies.Any();
    public int TotalAllergies => TotalItems;
    
    // Pagination properties
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int PageSize { get; set; } = 30;
    public int TotalItems { get; set; } = 0;
    
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
}

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
