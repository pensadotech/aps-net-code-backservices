using System.Threading.Channels;

namespace TennisBookings.ScoreProcessor.Sqs;

public interface ISqsMessageChannel
{
	ChannelReader<Message> Reader { get; }
	Task WriteMessagesAsync(IList<Message> messages, CancellationToken cancellationToken = default);
	void CompleteWriter(Exception? ex = null);
	bool TryCompleteWriter(Exception? ex = null);
}
