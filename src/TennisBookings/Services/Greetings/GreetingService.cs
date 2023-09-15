using System.Text.Json;
using Microsoft.Extensions.Options;

namespace TennisBookings.Services.Greetings;

public class GreetingService : IHomePageGreetingService, IGreetingService
{
	private static readonly ThreadLocal<Random> Random = new(() => new Random());

	private GreetingConfiguration _greetingConfiguration;
	//private readonly IOptionsMonitor<GreetingConfiguration> _greetingConfiguration;

	public GreetingService(
		IWebHostEnvironment hostEnvironment,
		ILogger<GreetingService> logger,
		IOptionsMonitor<GreetingConfiguration> options)
	{
		var contentRootPath = hostEnvironment.WebRootPath;

		var greetingsJson = File.ReadAllText(contentRootPath + "/greetings.json");

		var greetingsData = JsonSerializer.Deserialize<GreetingData>(greetingsJson);

		if (greetingsData is not null)
		{
			Greetings = greetingsData.Greetings;
			LoginGreetings = greetingsData.LoginGreetings;
		}

		_greetingConfiguration = options.CurrentValue;

		options.OnChange(config =>
		{
			_greetingConfiguration = config;
			logger.LogInformation("The greeting configuration has been updated");
		});
	}

	public string[] Greetings { get; } = Array.Empty<string>();

	public string[] LoginGreetings { get; } = Array.Empty<string>();

	public string GreetingColour => _greetingConfiguration.GreetingColour;

	[Obsolete("Prefer the GetRandomGreeting method defined in IGreetingService")]
	public string GetRandomHomePageGreeting()
	{
		return GetRandomGreeting();
	}

	public string GetRandomGreeting()
	{
		return GetRandomValue(Greetings);
	}

	public string GetRandomLoginGreeting(string name)
	{
		var loginGreeting = GetRandomValue(LoginGreetings);

		return loginGreeting.Replace("{name}", name);
	}

	private static string GetRandomValue(IReadOnlyList<string> greetings)
	{
		if (greetings.Count == 0)
			return string.Empty;

		var greetingToUse = Random.Value!.Next(greetings.Count);

		return greetingToUse >= 0 ? greetings[greetingToUse] : string.Empty;
	}

	private class GreetingData
	{
		public string[] Greetings { get; set; } = Array.Empty<string>();
		public string[] LoginGreetings { get; set; } = Array.Empty<string>();
	}
}
