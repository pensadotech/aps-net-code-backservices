namespace TennisBookings.ResultsProcessing.ExternalServices.Players;

public interface ITennisPlayerApiClient
{
	Task<TennisPlayer?> GetPlayerAsync(int id, CancellationToken cancellationToken = default);
}
