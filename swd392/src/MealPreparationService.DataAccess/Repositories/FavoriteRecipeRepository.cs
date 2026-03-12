using MealPreparationService.DataAccess.Data;
using MealPreparationService.Domain.Entities;

namespace MealPreparationService.DataAccess.Repositories;

public class FavoriteRecipeRepository : Repository<Recipe>, IFavoriteRecipeRepository
{
    public FavoriteRecipeRepository(ApplicationDbContext context) : base(context)
    {
    }
}
