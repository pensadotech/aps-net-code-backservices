using System.Net;
using TennisBookings.ScoreProcessor.Logging;

namespace TennisBookings.ScoreProcessor.BackgroundServices;

public class QueueReadingService : BackgroundService
{
	private readonly ILogger<QueueReadingService> _logger;
	private readonly ISqsMessageQueue _sqsMessageQueue;
	private readonly ISqsMessageChannel _sqsMessageChannel;
	private readonly string _queueUrl;

	public long ReceivesAttempted { get; private set; }
	public long MessagesReceived { get; private set; }

	public QueueReadingService(
		ILogger<QueueReadingService> logger,
		ISqsMessageQueue sqsMessageQueue,
		IOptions<AwsServicesConfiguration> options,
		ISqsMessageChannel sqsMessageChannel
		)
	{
		_logger = logger;
		_sqsMessageQueue = sqsMessageQueue;
		_sqsMessageChannel = sqsMessageChannel;
		if (options.Value.UseLocalStack)
		{
			_queueUrl = options.Value.LocalstackScoresQueueUrl;
		}
		else
		{
			_queueUrl = options.Value.ScoresQueueUrl;
		}

		_logger.LogInformation("Reading from {QueueUrl}", _queueUrl);
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("Started queue reading service.");

		// Register in logs that application is shuting down.
		// for production this can include metrics or advance info
		// Or fire an external event
		stoppingToken.Register(() =>
		{
			_logger.LogInformation("Ending queue reading service due to host shutdown");
		});

		var receiveMessageRequest = new ReceiveMessageRequest
		{
			QueueUrl = _queueUrl,
			MaxNumberOfMessages = 10,
			WaitTimeSeconds = 5
		};


		// wrap the main WHILE loop in a TRY-CATCH statement to handle exceptions
		try
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				ReceivesAttempted++;

				var receiveMessageResponse =
					await _sqsMessageQueue.ReceiveMessageAsync(receiveMessageRequest, stoppingToken);

				if (receiveMessageResponse.HttpStatusCode == HttpStatusCode.OK &&
					receiveMessageResponse.Messages.Any())
				{
					MessagesReceived += receiveMessageResponse.Messages.Count;

					_logger.LogInformation("Received {MessageCount} messages from the queue.",
						receiveMessageResponse.Messages.Count);

					// Producer for messages into the bounded channel
					await _sqsMessageChannel.WriteMessagesAsync(receiveMessageResponse.Messages, stoppingToken);
				}
				else if (receiveMessageResponse.HttpStatusCode == HttpStatusCode.OK)
				{
					_logger.LogInformation("No messages received. Attempting receive again in 10 seconds.",
						receiveMessageResponse.Messages.Count);

					await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
				}
				else if (receiveMessageResponse.HttpStatusCode != HttpStatusCode.OK)
				{
					_logger.LogError("Unsuccessful response from AWS SQS.");
				}
			}
		}
		catch (OperationCanceledException)       // <<------ exception handling 
		{
			_logger.OperationCancelledExceptionOccurred();
		}
		catch (Exception ex)                    // <<------ exception handling 
		{
			_logger.LogCritical(ex, "A critical exception was thrown.");
		}
		finally
		{
			_sqsMessageChannel.TryCompleteWriter();

		    // Stop is not included with producer, to allow the client service to flush out any messages
			// in the Bounded channel
		}
	}
}
