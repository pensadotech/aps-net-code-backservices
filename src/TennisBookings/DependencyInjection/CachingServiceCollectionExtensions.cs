using Microsoft.Extensions.DependencyInjection.Extensions;

namespace TennisBookings.DependencyInjection;

public static class CachingServiceCollectionExtensions
{
	public static IServiceCollection AddCaching(this IServiceCollection services)
	{
		// Adds a default implementation of IDistributedCache that stores items in memory to the IServiceCollection
		services.AddDistributedMemoryCache();

		// add wrapeer for using MS distributed cache and generic
		services.TryAddSingleton(typeof(IDistributedCache<>), typeof(DistributedCache<>)); // open generic registration

		// what is this used for ???? it helps obtaib=ing teh cache
		services.TryAddSingleton<IDistributedCacheFactory, DistributedCacheFactory>();

		return services;
	}
}
