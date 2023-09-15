namespace TennisBookings.ScoreProcessor.Sqs;

public sealed class SqsMessageDeleter : ISqsMessageDeleter
{
	private readonly IAmazonSQS _amazonSqs;
	private readonly string _queueUrl;

	public SqsMessageDeleter(
		IAmazonSQS amazonSqs,
		IOptions<AwsServicesConfiguration> options)
	{
		_amazonSqs = amazonSqs;

		if (options.Value.UseLocalStack)
		{
			_queueUrl = options.Value.LocalstackScoresQueueUrl;
		}
		else
		{
			_queueUrl = options.Value.ScoresQueueUrl;
		}
	}

	public async Task DeleteMessageAsync(Message message)
	{
		var sqsDeleteRequest = new DeleteMessageRequest
		{
			ReceiptHandle = message.ReceiptHandle,
			QueueUrl = _queueUrl
		};

		await _amazonSqs.DeleteMessageAsync(sqsDeleteRequest);
	}
}
