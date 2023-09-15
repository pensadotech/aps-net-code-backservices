using TennisBookings.External.Models;
using Microsoft.Extensions.Options;

namespace TennisBookings.External;

public class WeatherApiClient : IWeatherApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WeatherApiClient> _logger;

	private readonly bool _isConfigured = false;

    public WeatherApiClient(
		HttpClient httpClient,
		IOptionsMonitor<ExternalServicesConfiguration> options,
		ILogger<WeatherApiClient> logger)
    {
		var externalServicesConfig = options.Get(ExternalServicesConfiguration.WeatherApi);

        var url = externalServicesConfig.Url;

		if (!string.IsNullOrEmpty(url))
		{
			_isConfigured = true;
			httpClient.BaseAddress = new Uri(url);
		}

        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<WeatherApiResult?> GetWeatherForecastAsync(string city, CancellationToken cancellationToken = default)
    {
		if (!_isConfigured)
			return null;

        var path = $"api/currentWeather/{city}";

        try
        {
			// Call to WaetherService.Ap1
            var response = await _httpClient.GetAsync(path, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsAsync<WeatherApiResult>(cancellationToken);

            return content;
        }
        catch (HttpRequestException e)
        {
            _logger.LogError(e, "Failed to get weather data from API");
        }

        return null;
    }
}
