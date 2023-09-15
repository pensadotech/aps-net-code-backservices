namespace TennisBookings.ScoreProcessor.Logging;

public static class LoggerExtensions
{
	public static class EventIds
	{
		public static readonly EventId ExceptionCaught = new(1000, "ExceptionCaught");
		public static readonly EventId OperationCancelledExceptionCaught = new(1001, "OperationCancelledExceptionCaught");
	}

	public static void ExceptionOccurred(this ILogger logger, Exception ex) =>
		logger.Log(LogLevel.Error, EventIds.ExceptionCaught, ex, "An exception occurred and was caught.");

	public static void OperationCancelledExceptionOccurred(this ILogger logger) =>
		logger.Log(LogLevel.Information, EventIds.OperationCancelledExceptionCaught, "A task/operation cancelled exception was caught.");
}
