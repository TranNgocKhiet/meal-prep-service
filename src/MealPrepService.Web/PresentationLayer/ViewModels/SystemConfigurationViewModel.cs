using System.ComponentModel.DataAnnotations;

namespace MealPrepService.Web.PresentationLayer.ViewModels;

public class SystemConfigurationViewModel
{
    [Required(ErrorMessage = "Maximum meal plans is required")]
    [Range(1, 100, ErrorMessage = "Maximum meal plans must be between 1 and 100")]
    [Display(Name = "Max Meal Plans per Customer")]
    public int MaxMealPlansPerCustomer { get; set; }

    [Required(ErrorMessage = "Maximum fridge items is required")]
    [Range(1, 1000, ErrorMessage = "Maximum fridge items must be between 1 and 1000")]
    [Display(Name = "Max Fridge Items per Customer")]
    public int MaxFridgeItemsPerCustomer { get; set; }
}
