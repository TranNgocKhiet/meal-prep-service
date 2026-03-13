using MealPreparationService.DataAccess.Data;
using MealPreparationService.Domain.Entities;

namespace MealPreparationService.DataAccess.Repositories;

public class GoogleAuthRepository : Repository<GoogleAuth>, IGoogleAuthRepository
{
    public GoogleAuthRepository(ApplicationDbContext context) : base(context)
    {
    }
}
