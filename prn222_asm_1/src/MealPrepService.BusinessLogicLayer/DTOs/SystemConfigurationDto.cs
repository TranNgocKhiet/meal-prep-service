namespace MealPrepService.BusinessLogicLayer.DTOs;

public class SystemConfigurationDto
{
    public Guid Id { get; set; }
    public string ConfigKey { get; set; } = string.Empty;
    public string ConfigValue { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}
