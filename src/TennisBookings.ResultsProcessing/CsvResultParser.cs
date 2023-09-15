using System.Globalization;
using CsvHelper;

namespace TennisBookings.ResultsProcessing;

public class CsvResultParser : ICsvResultParser
{
	public IReadOnlyCollection<TennisMatchRow> ParseResult(Stream stream)
	{
		using var reader = new StreamReader(stream);
		using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

		var records = csv.GetRecords<TennisMatchRow>();

		return records.ToArray();
	}
}
