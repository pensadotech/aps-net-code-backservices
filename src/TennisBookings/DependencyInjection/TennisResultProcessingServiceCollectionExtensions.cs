using TennisBookings.Processing;
using TennisBookings.ResultsProcessing;
using TennisBookings.Web.BackgroundServices;

namespace TennisBookings.DependencyInjection;

public static class TennisResultProcessingServiceCollectionExtensions
{
	public static IServiceCollection AddTennisResultProcessing(this IServiceCollection services,
		IConfiguration config)
	{
		services
			.AddResultProcessing()
			.AddTennisPlayerApiClient(options =>
				options.BaseAddress = config.GetSection("ExternalServices:TennisPlayersApi")["Url"])
			.AddStatisticsApiClient(options =>
				options.BaseAddress = config.GetSection("ExternalServices:StatisticsApi")["Url"])
			.AddSingleton<FileProcessingChannel>()      // add fille processing using chanels
		    .AddHostedService<FileProcessingService>();

		return services;
	}
}
