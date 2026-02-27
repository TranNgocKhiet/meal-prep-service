namespace MealPrepService.DataAccessLayer.Entities;

public class SystemConfiguration : BaseEntity
{
    public string ConfigKey { get; set; } = string.Empty;
    public string ConfigValue { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
}
