using System.ComponentModel.DataAnnotations;

namespace MealPreparationService.API.Models.DTOs;

public class GoogleLoginDto
{
    [Required(ErrorMessage = "Google token is required")]
    public string GoogleToken { get; set; } = string.Empty;
}
