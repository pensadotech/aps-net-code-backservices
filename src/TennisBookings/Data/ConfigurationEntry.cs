using System.ComponentModel.DataAnnotations;

namespace TennisBookings.Data;

public class ConfigurationEntry
{
	[Key]
	public string Key { get; set; } = string.Empty;
	public string Value { get; set; } = string.Empty;
}
