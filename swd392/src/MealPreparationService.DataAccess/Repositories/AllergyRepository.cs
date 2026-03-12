using MealPreparationService.DataAccess.Data;
using MealPreparationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MealPreparationService.DataAccess.Repositories;

public class AllergyRepository : Repository<Allergy>, IAllergyRepository
{
    public AllergyRepository(ApplicationDbContext context) : base(context)
    {
    }

    // TODO: Update to use HealthProfileAllergies instead of removed UserAllergies table
    // public async Task<List<Allergy>> GetUserAllergiesAsync(string userId)
    // {
    //     return await _context.UserAllergies
    //         .Include(ua => ua.Allergy)
    //         .Where(ua => ua.UserId == userId)
    //         .Select(ua => ua.Allergy)
    //         .ToListAsync();
    // }

    // public async Task<UserAllergy?> GetUserAllergyAsync(string userId, string allergyId)
    // {
    //     return await _context.UserAllergies
    //         .FirstOrDefaultAsync(ua => ua.UserId == userId && ua.AllergyId == allergyId);
    // }

    // public async Task AddUserAllergyAsync(UserAllergy userAllergy)
    // {
    //     await _context.UserAllergies.AddAsync(userAllergy);
    //     await _context.SaveChangesAsync();
    // }

    // public async Task RemoveUserAllergyAsync(string userId, string allergyId)
    // {
    //     var userAllergy = await GetUserAllergyAsync(userId, allergyId);
    //     if (userAllergy != null)
    //     {
    //         _context.UserAllergies.Remove(userAllergy);
    //         await _context.SaveChangesAsync();
    //     }
    // }
}
