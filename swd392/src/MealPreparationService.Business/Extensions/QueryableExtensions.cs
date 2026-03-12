using Microsoft.EntityFrameworkCore;
using MealPreparationService.Business.Models;

namespace MealPreparationService.Business.Extensions;

/// <summary>
/// Extension methods for IQueryable to support pagination
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Applies pagination to a queryable and returns a paginated result
    /// </summary>
    public static async Task<PaginatedResult<T>> ToPaginatedResultAsync<T>(
        this IQueryable<T> query,
        PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        var totalCount = await query.CountAsync(cancellationToken);
        
        var items = await query
            .Skip(pagination.Skip)
            .Take(pagination.Take)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<T>(items, totalCount, pagination.PageNumber, pagination.PageSize);
    }

    /// <summary>
    /// Applies pagination to a queryable
    /// </summary>
    public static IQueryable<T> Paginate<T>(this IQueryable<T> query, PaginationParameters pagination)
    {
        return query
            .Skip(pagination.Skip)
            .Take(pagination.Take);
    }
}
