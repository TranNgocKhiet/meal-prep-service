using MealPreparationService.DataAccess.Data;
using MealPreparationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MealPreparationService.DataAccess.Repositories;

public class NutrientRepository : Repository<Nutrient>, INutrientRepository
{
    public NutrientRepository(ApplicationDbContext context) : base(context)
    {
    }

}
