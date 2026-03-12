using MealPreparationService.DataAccess.Data;
using MealPreparationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MealPreparationService.DataAccess.Repositories;

public class AccountRepository : Repository<Account>, IAccountRepository
{
    public AccountRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Account?> GetByEmailAsync(string email)
    {
        return await _dbSet
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<Account?> GetByGoogleIdAsync(string googleId)
    {
        return await _dbSet
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.GoogleAuthId == googleId);
    }

    public async Task<List<Account>> GetByRoleAsync(int roleId)
    {
        return await _dbSet
            .Include(u => u.Role)
            .Where(u => u.RoleId == roleId)
            .ToListAsync();
    }

    public override async Task<Account?> GetByIdAsync(string id)
    {
        return await _dbSet
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == id);
    }
}
