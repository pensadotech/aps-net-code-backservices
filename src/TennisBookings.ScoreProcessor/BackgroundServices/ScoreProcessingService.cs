using System.Diagnostics;
using TennisBookings.ScoreProcessor.Logging;
using TennisBookings.ScoreProcessor.Processing;

namespace TennisBookings.ScoreProcessor.BackgroundServices;

public class ScoreProcessingService : BackgroundService
{
	private readonly ILogger<ScoreProcessingService> _logger;
	private readonly ISqsMessageChannel _sqsMessageChannel;
	private readonly IServiceProvider _serviceProvider;
	private readonly IHostApplicationLifetime _hostApplicationLifetime;

	public ScoreProcessingService(
		ILogger<ScoreProcessingService> logger,
		ISqsMessageChannel sqsMessageChannel,
		IServiceProvider serviceProvider,
		IHostApplicationLifetime hostApplicationLifetime
	   )
	{
		_logger = logger;
		_sqsMessageChannel = sqsMessageChannel;
		_serviceProvider = serviceProvider;
		_hostApplicationLifetime = hostApplicationLifetime;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		await Task.Yield();  // To force it to become async from the beginning

		//Thread.Sleep(5000); // simulate long-running synchronous work

		try
		{
			//await Task.Delay(200);
			//throw new Exception("Oh no!!!");

			await foreach (var message in _sqsMessageChannel.Reader.ReadAllAsync()
			.WithCancellation(stoppingToken))
			{
				_logger.LogInformation("Read message {Id} to process from channel.", message.MessageId);

				using var scope = _serviceProvider.CreateScope();

				var scoreProcessor = scope.ServiceProvider
					.GetRequiredService<IScoreProcessor>();

				await scoreProcessor.ProcessScoresFromMessageAsync(message, stoppingToken);

				_logger.LogInformation("Finished processing message {Id} from channel.", message.MessageId);
			}

			_logger.LogInformation("Finished processing all available messages from channel.");
		}
		catch (OperationCanceledException)
		{
			_logger.OperationCancelledExceptionOccurred();
		}
		catch (Exception ex)
		{
			_logger.LogCritical(ex, "An unhandled exception was thrown. " +
				"Triggering app shutdown.");
		}
		finally
		{
			// Client Only!
			// send stop signal to other background services 
			// avoiding filling up the BoundedChannel with unatended messages
			_hostApplicationLifetime.StopApplication();
		}
	}

	public override async Task StopAsync(CancellationToken cancellationToken)
	{
		var sw = Stopwatch.StartNew();

		await base.StopAsync(cancellationToken);

		_logger.LogInformation("Completed shutdown in {Ms}ms", sw.ElapsedMilliseconds);
	}
}
