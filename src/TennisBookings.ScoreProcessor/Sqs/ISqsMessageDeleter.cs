namespace TennisBookings.ScoreProcessor.Sqs;

public interface ISqsMessageDeleter
{
	Task DeleteMessageAsync(Message message);
}
