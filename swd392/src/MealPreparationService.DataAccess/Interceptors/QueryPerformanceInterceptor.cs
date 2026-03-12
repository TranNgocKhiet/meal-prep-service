using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Data.Common;
using System.Diagnostics;

namespace MealPreparationService.DataAccess.Interceptors;

/// <summary>
/// Interceptor for logging database query performance
/// Validates: Requirements 23.1
/// </summary>
public class QueryPerformanceInterceptor : DbCommandInterceptor
{
    private readonly ILogger<QueryPerformanceInterceptor> _logger;
    private const int SlowQueryThresholdMs = 100; // 100ms threshold for database queries

    public QueryPerformanceInterceptor(ILogger<QueryPerformanceInterceptor> logger)
    {
        _logger = logger;
    }

    public override async ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        var elapsed = eventData.Duration.TotalMilliseconds;

        if (elapsed > SlowQueryThresholdMs)
        {
            _logger.LogWarning(
                "Slow database query detected | Duration: {Duration}ms | Threshold: {Threshold}ms | Query: {Query}",
                elapsed, SlowQueryThresholdMs, command.CommandText);
        }
        else
        {
            _logger.LogDebug(
                "Database query executed | Duration: {Duration}ms | Query: {Query}",
                elapsed, command.CommandText);
        }

        return await base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override DbDataReader ReaderExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result)
    {
        var elapsed = eventData.Duration.TotalMilliseconds;

        if (elapsed > SlowQueryThresholdMs)
        {
            _logger.LogWarning(
                "Slow database query detected | Duration: {Duration}ms | Threshold: {Threshold}ms | Query: {Query}",
                elapsed, SlowQueryThresholdMs, command.CommandText);
        }
        else
        {
            _logger.LogDebug(
                "Database query executed | Duration: {Duration}ms | Query: {Query}",
                elapsed, command.CommandText);
        }

        return base.ReaderExecuted(command, eventData, result);
    }

    public override async ValueTask<int> NonQueryExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        var elapsed = eventData.Duration.TotalMilliseconds;

        if (elapsed > SlowQueryThresholdMs)
        {
            _logger.LogWarning(
                "Slow database command detected | Duration: {Duration}ms | Threshold: {Threshold}ms | Command: {Command}",
                elapsed, SlowQueryThresholdMs, command.CommandText);
        }
        else
        {
            _logger.LogDebug(
                "Database command executed | Duration: {Duration}ms | Command: {Command}",
                elapsed, command.CommandText);
        }

        return await base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override int NonQueryExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result)
    {
        var elapsed = eventData.Duration.TotalMilliseconds;

        if (elapsed > SlowQueryThresholdMs)
        {
            _logger.LogWarning(
                "Slow database command detected | Duration: {Duration}ms | Threshold: {Threshold}ms | Command: {Command}",
                elapsed, SlowQueryThresholdMs, command.CommandText);
        }
        else
        {
            _logger.LogDebug(
                "Database command executed | Duration: {Duration}ms | Command: {Command}",
                elapsed, command.CommandText);
        }

        return base.NonQueryExecuted(command, eventData, result);
    }
}
