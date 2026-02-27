namespace MealPrepService.BusinessLogicLayer.DTOs;

public class AllergyDto
{
    public Guid Id { get; set; }
    public string AllergyName { get; set; } = string.Empty;
    public bool IsSelected { get; set; } // For UI selection in forms
}

public class CreateAllergyDto
{
    public string AllergyName { get; set; } = string.Empty;
}

public class UpdateAllergyDto
{
    public Guid Id { get; set; }
    public string AllergyName { get; set; } = string.Empty;
}
