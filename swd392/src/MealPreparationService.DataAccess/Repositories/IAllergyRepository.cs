using MealPreparationService.Domain.Entities;

namespace MealPreparationService.DataAccess.Repositories;

public interface IAllergyRepository : IRepository<Allergy>
{
    // TODO: Update to use HealthProfileAllergies instead of removed UserAllergies table
    // Task<List<Allergy>> GetUserAllergiesAsync(string userId);
    // Task<UserAllergy?> GetUserAllergyAsync(string userId, string allergyId);
    // Task AddUserAllergyAsync(UserAllergy userAllergy);
    // Task RemoveUserAllergyAsync(string userId, string allergyId);
}
