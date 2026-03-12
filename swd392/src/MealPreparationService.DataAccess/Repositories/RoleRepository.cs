using MealPreparationService.DataAccess.Data;
using MealPreparationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MealPreparationService.DataAccess.Repositories;

public class RoleRepository : IRoleRepository
{
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<Role> _dbSet;

    public RoleRepository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = context.Set<Role>();
    }

    public async Task<Role?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<Role?> GetByNameAsync(string name)
    {
        return await _dbSet.FirstOrDefaultAsync(r => r.Name == name);
    }

    public async Task<List<Role>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public async Task<Role> AddAsync(Role entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<Role> UpdateAsync(Role entity)
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
