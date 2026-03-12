using MealPreparationService.DataAccess.Data;
using MealPreparationService.Domain.Entities;

namespace MealPreparationService.DataAccess.Repositories;

public class NutrientCalculationRepository : Repository<NutrientCalculation>, INutrientCalculationRepository
{
    public NutrientCalculationRepository(ApplicationDbContext context) : base(context)
    {
    }
}
