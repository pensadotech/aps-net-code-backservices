using Amazon.S3.Model;
using Microsoft.Win32;
using System.Reflection.Metadata;
using System;
using TennisBookings.Processing;
using TennisBookings.ResultsProcessing;

namespace TennisBookings.Web.BackgroundServices;

public class FileProcessingService : BackgroundService
{
	private readonly ILogger<FileProcessingService> _logger;
	private readonly FileProcessingChannel _fileProcessingChannel;
	private readonly IServiceProvider _serviceProvider;

	public FileProcessingService(
		ILogger<FileProcessingService> logger,
		FileProcessingChannel boundedMessageChannel,
		IServiceProvider serviceProvider)
	{
		_logger = logger;
		_fileProcessingChannel = boundedMessageChannel;
		_serviceProvider = serviceProvider;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		// The ExecuteAsync task will use the read capabilities inside the _fileProcessingChannel. This return an IEnumerable
		// that can be used to obtain each file in the channel.
		// It is important to observe is that the TennisBookings.ResultsProcessing.ResultProcessor is defined in the ServiceCollectionExtensions
        // as Scoped, because it depend on other scoped services(services.TryAddScoped<IResultProcessor, ResultProcessor>()).
		// However the FileProcessingService is registerd is registered as AddHostedService<T> which implies as a Singletonregistration, 
        // and thefore injecting a scoped service will create a viloation that will cuase to throw an error on execution.
		// For this, the ResultProcessor will be registerd inside the loop to limit the Scoped service life. 
        // To do this is required to inject an IServiceProvider.
		// Normally, the use of IServiceProvider is an antipattern, but for this case, it is teh best solution to handle
        // a scoped services inside a singleton service.

		// From file processor, read a list of files conyained in the channel
		await foreach (var fileName in _fileProcessingChannel.ReadAllAsync()
			.WithCancellation(stoppingToken))
		{
			// Create a scoped service that will live for the duroation of teh loop
			using var scope = _serviceProvider.CreateScope();

			// request for an instance of the IResultProcessor
			var processor = scope.ServiceProvider.GetRequiredService<IResultProcessor>();

			try
			{
				// Get file contents from stream
				await using var stream = File.OpenRead(fileName);

				//Process contents
				await processor.ProcessAsync(stream, stoppingToken);
			}
			finally
			{
				File.Delete(fileName); // Delete the temp file always
			}
		}
	}

	internal static class EventIds
	{
		public static readonly EventId StartedProcessing = new(100, "StartedProcessing");
		public static readonly EventId ProcessorStopping = new(101, "ProcessorStopping");
		public static readonly EventId StoppedProcessing = new(102, "StoppedProcessing");
		public static readonly EventId ProcessedMessage = new(110, "ProcessedMessage");
	}

	private static class Log
	{
		private static readonly Action<ILogger, string, Exception?> _processedMessage = LoggerMessage.Define<string>(
			LogLevel.Debug,
			EventIds.ProcessedMessage,
			"Read and processed message with ID '{MessageId}' from the channel.");

		public static void StartedProcessing(ILogger logger) =>
			logger.Log(LogLevel.Trace, EventIds.StartedProcessing, "Started message processing service.");

		public static void ProcessorStopping(ILogger logger) =>
			logger.Log(LogLevel.Information, EventIds.ProcessorStopping, "Message processing stopping due to app termination!");

		public static void StoppedProcessing(ILogger logger) =>
			logger.Log(LogLevel.Trace, EventIds.StoppedProcessing, "Stopped message processing service.");

		public static void ProcessedMessage(ILogger logger, string messageId) =>
			_processedMessage(logger, messageId, null);
	}
}
