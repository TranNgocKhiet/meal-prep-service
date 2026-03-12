using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MealPreparationService.Business.Services;

/// <summary>
/// In-memory cache service implementation with 24-hour default expiration
/// </summary>
public class CacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CacheService> _logger;
    private readonly ConcurrentDictionary<string, byte> _keys;
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromHours(24);

    public CacheService(IMemoryCache cache, ILogger<CacheService> logger)
    {
        _cache = cache;
        _logger = logger;
        _keys = new ConcurrentDictionary<string, byte>();
    }

    /// <summary>
    /// Gets a cached value by key
    /// </summary>
    public Task<T?> GetAsync<T>(string key) where T : class
    {
        try
        {
            if (_cache.TryGetValue(key, out string? cachedJson) && cachedJson != null)
            {
                var value = JsonSerializer.Deserialize<T>(cachedJson);
                _logger.LogDebug("Cache hit for key: {Key}", key);
                return Task.FromResult(value);
            }

            _logger.LogDebug("Cache miss for key: {Key}", key);
            return Task.FromResult<T?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving from cache for key: {Key}", key);
            return Task.FromResult<T?>(null);
        }
    }

    /// <summary>
    /// Sets a cached value with expiration (defaults to 24 hours if not specified)
    /// </summary>
    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
    {
        try
        {
            var json = JsonSerializer.Serialize(value);
            var cacheExpiration = expiration ?? DefaultExpiration;
            
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = cacheExpiration,
                SlidingExpiration = null // No sliding expiration, only absolute
            };

            cacheOptions.RegisterPostEvictionCallback((k, v, r, s) =>
            {
                _keys.TryRemove(k.ToString()!, out _);
                _logger.LogDebug("Cache entry evicted: {Key}, Reason: {Reason}", k, r);
            });

            _cache.Set(key, json, cacheOptions);
            _keys.TryAdd(key, 0);
            
            _logger.LogDebug("Cached value for key: {Key} with expiration: {Expiration}", key, cacheExpiration);
            
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache for key: {Key}", key);
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Removes a cached value by key
    /// </summary>
    public Task RemoveAsync(string key)
    {
        try
        {
            _cache.Remove(key);
            _keys.TryRemove(key, out _);
            _logger.LogDebug("Removed cache for key: {Key}", key);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache for key: {Key}", key);
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Removes all cached values matching a pattern
    /// </summary>
    public Task RemoveByPatternAsync(string pattern)
    {
        try
        {
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);
            var keysToRemove = _keys.Keys.Where(k => regex.IsMatch(k)).ToList();

            foreach (var key in keysToRemove)
            {
                _cache.Remove(key);
                _keys.TryRemove(key, out _);
            }

            _logger.LogDebug("Removed {Count} cached values matching pattern: {Pattern}", keysToRemove.Count, pattern);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cached values by pattern: {Pattern}", pattern);
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Checks if a key exists in cache
    /// </summary>
    public Task<bool> ExistsAsync(string key)
    {
        try
        {
            var exists = _cache.TryGetValue(key, out _);
            return Task.FromResult(exists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking cache existence for key: {Key}", key);
            return Task.FromResult(false);
        }
    }
}
