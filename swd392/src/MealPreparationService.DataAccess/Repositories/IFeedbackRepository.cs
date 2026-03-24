using MealPreparationService.Domain.Entities;

namespace MealPreparationService.DataAccess.Repositories;

public interface IFeedbackRepository : IRepository<Feedback>
{
    Task<List<Feedback>> GetByCustomerIdAsync(string customerId);
    Task<List<Feedback>> GetAllFeedbacksAsync(int skip = 0, int take = 10);
}
