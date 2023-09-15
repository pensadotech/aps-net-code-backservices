namespace TennisBookings.Configuration;

public class ExternalServicesConfiguration
{
	public const string WeatherApi = "WeatherApi";
	public const string ProductsApi = "ProductsApi";

	public string Url { get; set; } = string.Empty;
	public int MinsToCache { get; set; } = 10;
	public string ApiKey { get; set; } = string.Empty;
}
