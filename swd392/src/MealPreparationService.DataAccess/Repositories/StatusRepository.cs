using MealPreparationService.DataAccess.Data;
using MealPreparationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MealPreparationService.DataAccess.Repositories;

public class StatusRepository : IStatusRepository
{
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<Status> _dbSet;

    public StatusRepository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = context.Set<Status>();
    }

    public async Task<Status?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<Status?> GetByNameAsync(string name)
    {
        return await _dbSet.FirstOrDefaultAsync(s => s.Name == name);
    }

    public async Task<List<Status>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public async Task<Status> AddAsync(Status entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<Status> UpdateAsync(Status entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null)
        {
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _dbSet.AnyAsync(e => e.Id == id);
    }

    public async Task<int> CountAsync()
    {
        return await _dbSet.CountAsync();
    }
}
