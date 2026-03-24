using MealPreparationService.DataAccess.Data;
using MealPreparationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MealPreparationService.DataAccess.Repositories;

public class FeedbackRepository : Repository<Feedback>, IFeedbackRepository
{
    public FeedbackRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<Feedback>> GetByCustomerIdAsync(string customerId)
    {
        return await _context.Set<Feedback>()
            .Where(f => f.CustomerId == customerId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Feedback>> GetAllFeedbacksAsync(int skip = 0, int take = 10)
    {
        return await _context.Set<Feedback>()
            .Include(f => f.Customer)
            .OrderByDescending(f => f.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }
}
