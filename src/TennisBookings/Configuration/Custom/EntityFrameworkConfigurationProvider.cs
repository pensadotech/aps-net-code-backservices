namespace TennisBookings.Configuration.Custom;

public class EntityFrameworkConfigurationProvider : ConfigurationProvider
{
	public EntityFrameworkConfigurationProvider(Action<DbContextOptionsBuilder> optionsAction)
	{
		OptionsAction = optionsAction;
	}

	public Action<DbContextOptionsBuilder> OptionsAction { get; }

	public override void Load()
	{
		var builder = new DbContextOptionsBuilder<TennisBookingsDbContext>();

		OptionsAction(builder);

		using var dbContext = new TennisBookingsDbContext(builder.Options);

		dbContext.Database.EnsureCreated();

		Data = dbContext.ConfigurationEntries.Any()
			? dbContext.ConfigurationEntries.ToDictionary(entry => entry.Key,
				entry => entry.Value, StringComparer.OrdinalIgnoreCase)
			: new Dictionary<string, string>();
	}
}
