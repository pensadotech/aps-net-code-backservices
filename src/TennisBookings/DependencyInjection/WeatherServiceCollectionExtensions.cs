using Amazon.Runtime.Internal.Util;
using System.Threading;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TennisBookings.BackgroundServices;
using TennisBookings.External;
using TennisBookings.Services.Weather;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace TennisBookings.DependencyInjection;

public static class WeatherServiceCollectionExtensions
{
	public static IServiceCollection AddWeatherForecasting(this IServiceCollection services,
		IConfiguration config)
	{
		if (config.GetValue<bool>("Features:WeatherForecasting:EnableWeatherForecasting"))
		{
			// From the front end
			// CachedWeatherForecaster -> WeatherForecaster -> WeatherApiClient

			// The first three registered services are the ones used from the frontend. The third one can be
            // considered an auxiliar service that updates the cache in regular bases to make the applicaiton
            // more efficient.

			// Register the API client class that will be used in teh bacground service
			services.AddHttpClient<IWeatherApiClient, WeatherApiClient>(); // to call WeatherForcast.Api

			// Apply decorator patern by using Scrutor 'Decorate'
			// First register the regular class implementing IWeatherForecaster
			// then register the decorator class, taht will recevie a IWeatherForecaster
			// and will return a IWeatherForecaster, too
			services.TryAddSingleton<IWeatherForecaster, WeatherForecaster>(); // invokes WeatherApiClient methods
			services.Decorate<IWeatherForecaster, CachedWeatherForecaster>();  // read cache or calls API using WeatherForecaster


		    // Add an auxilar background service to collect weather and cache value
			services.AddHostedService<WeatherCacheService>();  
		}
		else
		{
			services.TryAddSingleton<IWeatherForecaster, DisabledWeatherForecaster>();
		}

		return services;
	}
}
