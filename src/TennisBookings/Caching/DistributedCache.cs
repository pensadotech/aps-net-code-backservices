using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace TennisBookings.Caching;

public class DistributedCache<T> : IDistributedCache<T>
{
	private readonly IDistributedCache _distributedCache;  // Microsfot distributed cache
	private readonly ILogger<DistributedCache<T>> _logger;  // logger

	private readonly string _cacheKeyPrefix;

	public DistributedCache(
		IDistributedCache distributedCache,  // recevies MS distributed cache
		ILogger<DistributedCache<T>> logger)
	{
		_distributedCache = distributedCache;
		_logger = logger;

		_cacheKeyPrefix = $"{typeof(T).Namespace}_{typeof(T).Name}_";
	}

	public async Task<(bool Found, T? Value)> TryGetValueAsync(string key)
	{
		var value = await GetAsync(key);

		return (value is not null, value);
	}

	public async Task<T?> GetAsync(string key)
	{
		// Return value from cache
		var cachedResult = await _distributedCache.GetStringAsync(CacheKey(key));

		return cachedResult == null ? default : DeserialiseFromString(cachedResult);
	}

	public async Task SetAsync(string key, T item, int minutesToCache)
	{
		var cacheEntryOptions = new DistributedCacheEntryOptions
		{ AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(minutesToCache) };

		var serialisedItemToCache = SerialiseForCaching(item);

		// Ste value on cache
		await _distributedCache.SetStringAsync(CacheKey(key), serialisedItemToCache, cacheEntryOptions);
	}

	public Task RemoveAsync(string key) => _distributedCache.RemoveAsync(CacheKey(key));

	private string CacheKey(string key) => $"{_cacheKeyPrefix}{key}";

	private T? DeserialiseFromString(string cachedResult)
	{
		try
		{
			return JsonSerializer.Deserialize<T>(cachedResult, new JsonSerializerOptions
			{
				MaxDepth = 10
			});
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to deserialise from cached string");
			return default;
		}
	}

	private string? SerialiseForCaching(T item)
	{
		if (item == null)
			return null;

		try
		{
			return JsonSerializer.Serialize(item);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to serialise type '{Type}' for caching", typeof(T).FullName);
			throw;
		}
	}
}
