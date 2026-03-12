using MealPreparationService.Domain.Entities;

namespace MealPreparationService.DataAccess.Repositories;

public interface IAccountRepository : IRepository<Account>
{
    Task<Account?> GetByEmailAsync(string email);
    Task<Account?> GetByGoogleIdAsync(string googleId);
    Task<List<Account>> GetByRoleAsync(int roleId);
}
