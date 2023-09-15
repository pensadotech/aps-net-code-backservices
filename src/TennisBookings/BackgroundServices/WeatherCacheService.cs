using Microsoft.Extensions.Options;
using TennisBookings.External;


namespace TennisBookings.BackgroundServices;

public class WeatherCacheService : BackgroundService
{
	private readonly IWeatherApiClient _weatherApiClient;
	private readonly IDistributedCache<WeatherResult> _cache;
	private readonly ILogger<WeatherCacheService> _logger;

	private readonly int _minutesToCache;
	private readonly int _refreshIntervalInSeconds;

	public WeatherCacheService(
		IWeatherApiClient weatherApiClient,
		IDistributedCache<WeatherResult> cache,
		IOptionsMonitor<ExternalServicesConfiguration> options,
		ILogger<WeatherCacheService> logger)
	{
		_weatherApiClient = weatherApiClient;
		_cache = cache;
		_logger = logger;
		_minutesToCache = options.Get(ExternalServicesConfiguration.WeatherApi).MinsToCache;
		_refreshIntervalInSeconds = _minutesToCache > 1 ? (_minutesToCache - 1) * 60 : 30;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			// Note: The cancelation token is passed to th method that
			//       that can accept it. At shutdown any async call will
			//       be cancelled.

			// Rest API call to obtain weather forecast 
			var forecast = await _weatherApiClient
				.GetWeatherForecastAsync("Eastbourne", stoppingToken);

			if (forecast is not null)
			{
				// if result is not null, create a result
				var currentWeather = new WeatherResult
				{
					City = "Eastbourne",
					Weather = forecast.Weather
				};

				// The key is necesary in order to store teh data in memory
				var cacheKey = $"current_weather_{DateTime.UtcNow:yyyy_MM_dd}";

				_logger.LogInformation("Updating weather in cache.");

				// result is added to the cache (IDistributed Cache)
				await _cache.SetAsync(cacheKey, currentWeather, _minutesToCache);
			}

			// Delays execution until teh defined interval has passed. 
			await Task.Delay(TimeSpan.FromSeconds(_refreshIntervalInSeconds),
				stoppingToken);
		}
	}
}
