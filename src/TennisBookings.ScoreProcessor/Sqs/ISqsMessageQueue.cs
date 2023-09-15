namespace TennisBookings.ScoreProcessor.Sqs;

public interface ISqsMessageQueue
{
	Task<ReceiveMessageResponse> ReceiveMessageAsync(
		ReceiveMessageRequest request,
		CancellationToken cancellationToken = default);
}
