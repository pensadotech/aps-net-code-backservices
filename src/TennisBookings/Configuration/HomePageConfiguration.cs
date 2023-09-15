using System.ComponentModel.DataAnnotations;

namespace TennisBookings.Configuration;

public class HomePageConfiguration
{
	public bool EnableGreeting { get; set; }
	public bool EnableWeatherForecast { get; set; }
	public string ForecastSectionTitle { get; set; } = string.Empty;
}
