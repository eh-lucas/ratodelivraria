using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Sherlock.Business.Interfaces;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Sherlock.Business.Services;

public class CacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<CacheService> _logger;
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromHours(2);

    public CacheService(IDistributedCache cache, ILogger<CacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        try
        {
            var data = await _cache.GetStringAsync(key);
            if (string.IsNullOrEmpty(data))
            {
                _logger.LogDebug("Cache miss para chave {CacheKey}", key);
                return null;
            }

            _logger.LogDebug("Cache hit para chave {CacheKey}", key);
            return JsonSerializer.Deserialize<T>(data);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao buscar cache para chave {CacheKey}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
    {
        try
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? DefaultExpiration
            };

            var data = JsonSerializer.Serialize(value);
            await _cache.SetStringAsync(key, data, options);

            _logger.LogDebug("Cache definido para chave {CacheKey} com TTL {TTL}",
                key, expiration ?? DefaultExpiration);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao definir cache para chave {CacheKey}", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await _cache.RemoveAsync(key);
            _logger.LogDebug("Cache removido para chave {CacheKey}", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao remover cache para chave {CacheKey}", key);
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        try
        {
            var data = await _cache.GetStringAsync(key);
            return !string.IsNullOrEmpty(data);
        }
        catch
        {
            return false;
        }
    }

    public string GenerateBookProviderKey(string bookTitle, int providerId)
    {
        var normalizedTitle = NormalizeString(bookTitle);
        return $"book:price:{GenerateHash(normalizedTitle)}:provider:{providerId}";
    }

    private static string NormalizeString(string input)
    {
        return input.ToLowerInvariant()
            .Trim()
            .Replace("  ", " ");
    }

    private static string GenerateHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes)[..16].ToLowerInvariant();
    }
}
