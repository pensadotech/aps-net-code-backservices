using System.Threading.Channels;

namespace TennisBookings.Processing;

// Channel is a simple way to coordinate the frontend with background serivices
// this program wraps the Channel funcitonality to expose only what is needed
// for the project.

public class FileProcessingChannel
{
	// max number of message that can exist in teh channel at one time
	private const int MaxMessagesInChannel = 100; 

	private readonly Channel<string> _channel;
	private readonly ILogger<FileProcessingChannel> _logger;

	public FileProcessingChannel(ILogger<FileProcessingChannel> logger)
	{
		// Create new bounded channel to hold string values
		var options = new BoundedChannelOptions(MaxMessagesInChannel)
		{
			SingleWriter = false, // to support multiple producers
			SingleReader = true   // Only one consumer
		};

		// String that will contain the temporary filename to upload.
		_channel = Channel.CreateBounded<string>(options);

		_logger = logger;
	}

	// Writes filename to a channel as long capacity is avalable
	public async Task<bool> AddFileAsync(string fileName, CancellationToken ct = default)
	{
		while (await _channel.Writer.WaitToWriteAsync(ct) && !ct.IsCancellationRequested)
		{
			if (_channel.Writer.TryWrite(fileName))
			{
				Log.ChannelMessageWritten(_logger, fileName);

				return true;
			}
		}

		return false;
	}

	// Reads fron a channel
	public IAsyncEnumerable<string> ReadAllAsync(CancellationToken ct = default) =>
		_channel.Reader.ReadAllAsync(ct);

	public bool TryCompleteWriter(Exception? ex = null) => _channel.Writer.TryComplete(ex);

	internal static class EventIds
	{
		public static readonly EventId ChannelMessageWritten = new(100, "ChannelMessageWritten");
	}

	private static class Log
	{
		private static readonly Action<ILogger, string, Exception?> _channelMessageWritten = LoggerMessage.Define<string>(
			LogLevel.Information,
			EventIds.ChannelMessageWritten,
			"Filename {FileName} was written to the channel.");

		public static void ChannelMessageWritten(ILogger logger, string fileName)
		{
			_channelMessageWritten(logger, fileName, null);
		}
	}
}
