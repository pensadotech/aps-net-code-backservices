using Microsoft.Extensions.Options;

namespace TennisBookings.Shared.Weather;

public class CachedWeatherForecaster : IWeatherForecaster
{
	private readonly IWeatherForecaster _weatherForecaster;
	private readonly IDistributedCache<WeatherResult> _cache;
	private readonly int _minsToCache;

	public CachedWeatherForecaster(
		IWeatherForecaster weatherForecaster,
		IDistributedCache<WeatherResult> cache,
		IOptionsMonitor<ExternalServicesConfiguration> options)
	{
		_weatherForecaster = weatherForecaster;
		_cache = cache;
		_minsToCache = options.Get(ExternalServicesConfiguration.WeatherApi).MinsToCache;
	}

	public bool ForecastEnabled => _weatherForecaster.ForecastEnabled;

	public async Task<WeatherResult> GetCurrentWeatherAsync(string city)
	{
		var cacheKey = $"current_weather_{DateTime.UtcNow:yyyy_MM_dd}";

		// use value from memory
		var (isCached, forecast) = await _cache.TryGetValueAsync(cacheKey);

		if (isCached)
			return forecast!;

		// Otherwise, call API again
		var result = await _weatherForecaster.GetCurrentWeatherAsync(city);

		// Update cache with recent value
		await _cache.SetAsync(cacheKey, result, minutesToCache: _minsToCache);

		return result;
	}
}
