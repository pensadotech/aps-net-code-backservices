namespace TennisBookings.ScoreProcessor.S3;

public interface IS3DataProvider
{
	Task<Stream> GetStreamAsync(
		string objectKey,
		CancellationToken cancellationToken = default);
}
