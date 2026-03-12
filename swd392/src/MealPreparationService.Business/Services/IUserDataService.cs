namespace MealPreparationService.Business.Services;

/// <summary>
/// Service for managing user data including deletion and anonymization
/// </summary>
public interface IUserDataService
{
    /// <summary>
    /// Anonymizes user personal data while retaining transaction records
    /// Validates: Requirements 22.7
    /// </summary>
    Task AnonymizeUserDataAsync(string userId);
    
    /// <summary>
    /// Deletes a user account and anonymizes their data
    /// Validates: Requirements 22.7
    /// </summary>
    Task DeleteUserAccountAsync(string userId);
}
