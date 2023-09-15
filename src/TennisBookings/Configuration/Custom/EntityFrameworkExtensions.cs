namespace TennisBookings.Configuration.Custom;

public static class EntityFrameworkExtensions
{
	public static IConfigurationBuilder AddEfConfiguration(this IConfigurationBuilder builder,
		Action<DbContextOptionsBuilder> optionsAction) =>
			builder.Add(new EntityFrameworkConfigurationSource(optionsAction));
}
