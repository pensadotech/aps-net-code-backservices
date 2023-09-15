namespace TennisBookings.ResultsProcessing;

public interface ICsvResultParser
{
	IReadOnlyCollection<TennisMatchRow> ParseResult(Stream stream);
}
