namespace TennisBookings.ScoreProcessor.Processing;

public interface IScoreProcessor
    {
        Task ProcessScoresFromMessageAsync(
			Message message,
			CancellationToken cancellationToken = default);
    }
