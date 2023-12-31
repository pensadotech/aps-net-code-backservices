namespace TennisBookings.ScoreProcessor.S3;

public class S3EventNotificationMessageParser : IS3EventNotificationMessageParser
{
	private readonly ILogger<S3EventNotificationMessageParser> _logger;

	public S3EventNotificationMessageParser(ILogger<S3EventNotificationMessageParser> logger) => _logger = logger;

	public IReadOnlyCollection<string> Parse(Message message)
	{
		var objectKeys = new List<string>();

		try
		{
			var snsJsonDocument = JsonDocument.Parse(message.Body);

			var messageString = snsJsonDocument.RootElement.GetProperty("Message").GetString();

			if (!string.IsNullOrEmpty(messageString))
			{
				var jsonDocument = JsonDocument.Parse(messageString);

				var records = jsonDocument.RootElement.GetProperty("Records").EnumerateArray();

				foreach (var record in records)
				{
					var eventName = record.GetProperty("eventName").GetString();

					if (!string.IsNullOrEmpty(eventName))
					{
						if (!eventName.Equals("ObjectCreated:Put", StringComparison.OrdinalIgnoreCase))
							continue;

						var objectKey = record
							.GetProperty("s3")
							.GetProperty("object")
							.GetProperty("key")
							.GetString();

						if (!string.IsNullOrEmpty(objectKey))
							objectKeys.Add(objectKey);
					}
				}
			}
		}
		catch (Exception e)
		{
			_logger.LogError(e, "Failed to parse S3 event notification " +
				"from SQS Message ID {SqsMessageId} with body {SqsMessage}", message.MessageId, message.Body);
		}

		return objectKeys;
	}
}
