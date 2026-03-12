using MealPreparationService.DataAccess.Data;
using MealPreparationService.Domain.Entities;

namespace MealPreparationService.DataAccess.Repositories;

public class SystemConfigurationRepository : Repository<SystemConfiguration>, ISystemConfigurationRepository
{
    public SystemConfigurationRepository(ApplicationDbContext context) : base(context)
    {
    }
}
