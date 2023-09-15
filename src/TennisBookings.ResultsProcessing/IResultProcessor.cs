namespace TennisBookings.ResultsProcessing;

public interface IResultProcessor
{
	Task ProcessAsync(Stream stream, CancellationToken cancellationToken = default);
}
