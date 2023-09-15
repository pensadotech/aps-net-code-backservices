using Microsoft.Extensions.Options;
using TennisBookings.Services;

namespace TennisBookings.Configuration;

public class HomePageConfigurationValidation : IValidateOptions<HomePageConfiguration>
{
	private readonly WeatherForecastingConfiguration _weatherConfig;
	private readonly IProfanityChecker _profanityChecker;
	private readonly bool _checkForProfanity;

	public HomePageConfigurationValidation(
		IContentConfiguration contentConfig,
		IOptions<WeatherForecastingConfiguration> weatherConfig,
		IProfanityChecker profanityChecker)
	{
		_checkForProfanity = contentConfig.CheckForProfanity;
		_weatherConfig = weatherConfig.Value;
		_profanityChecker = profanityChecker;
	}

	public ValidateOptionsResult Validate(string name, HomePageConfiguration options)
	{
		if (_weatherConfig.EnableWeatherForecast && options.EnableWeatherForecast
			&& string.IsNullOrEmpty(options.ForecastSectionTitle))
		{
			return ValidateOptionsResult.Fail("A title is required, " +
				"when the weather forecast is enabled.");
		}

		if (_checkForProfanity && _profanityChecker
			.ContainsProfanity(options.ForecastSectionTitle))
		{
			return ValidateOptionsResult.Fail("The configured title contains " +
				"a blocked profanity word.");
		}

		return ValidateOptionsResult.Success;
	}
}
