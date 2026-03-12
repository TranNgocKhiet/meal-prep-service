namespace MealPreparationService.Domain.Entities;

public class GoogleAuth : BaseEntity
{
    public string ProviderKey { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsVerified { get; set; }
    
    // Navigation properties
    public ICollection<Account> Accounts { get; set; } = new List<Account>();
}
