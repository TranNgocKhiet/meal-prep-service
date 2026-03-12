namespace MealPreparationService.Domain.Entities;

public class HealthProfileAllergy : BaseEntity
{
    public string HealthProfileId { get; set; } = string.Empty;
    public HealthProfile HealthProfile { get; set; } = null!;
    public string AllergyId { get; set; } = string.Empty;
    public Allergy Allergy { get; set; } = null!;
}
