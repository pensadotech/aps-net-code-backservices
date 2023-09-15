global using Amazon.S3;
global using Amazon.S3.Model;
global using Amazon.SQS;
global using Amazon.SQS.Model;
global using Microsoft.Extensions.Options;
global using System.Text.Json;
global using TennisBookings.ResultsProcessing;
global using TennisBookings.ScoreProcessor;
global using TennisBookings.ScoreProcessor.Sqs;
global using TennisBookings.ScoreProcessor.S3;
using TennisBookings.ScoreProcessor.BackgroundServices;
using TennisBookings.ScoreProcessor.Processing;

var host = Host.CreateDefaultBuilder(args)
	.ConfigureServices((hostContext, services) =>
	{
		// This is to modify the behavior to shutdown the application
		// in case of an exception in teh background service
		services.Configure<HostOptions>(hostOptions =>
		{
			hostOptions.BackgroundServiceExceptionBehavior =     // <<- Override behavior, do nto stop upon an error. 
				BackgroundServiceExceptionBehavior.Ignore;
			hostOptions.ShutdownTimeout = TimeSpan.FromSeconds(60);  // <<-- override default cancalation task wait time 
		});

		services.Configure<AwsServicesConfiguration>(hostContext.Configuration.GetSection("AWS"));

		services.AddAWSService<IAmazonSQS>();
		services.AddAWSService<IAmazonS3>();

		var useLocalStack = hostContext.Configuration.GetValue<bool>("AWS:UseLocalStack");

		if (hostContext.HostingEnvironment.IsDevelopment() && useLocalStack)
		{
			services.AddSingleton<IAmazonSQS>(sp =>
			{
				var s3Client = new AmazonSQSClient(new AmazonSQSConfig
				{
					ServiceURL = "http://localhost:4566",
					AuthenticationRegion = hostContext.Configuration.GetValue<string>("AWS:Region") ?? "eu-west-2"
				});

				return s3Client;
			});

			services.AddSingleton<IAmazonS3>(sp =>
			{
				var s3Client = new AmazonS3Client(new AmazonS3Config
				{
					ServiceURL = "http://localhost:4566",
					ForcePathStyle = true,
					AuthenticationRegion = hostContext.Configuration.GetValue<string>("AWS:Region") ?? "eu-west-2"
				});

				return s3Client;
			});
		}

		// additional services requied for ScoreProcessingService
		services.AddTransient<IScoreProcessor, AwsScoreProcessor>();
		services.AddSingleton<IS3EventNotificationMessageParser, S3EventNotificationMessageParser>();
		services.AddSingleton<IS3DataProvider, S3DataProvider>();
		services.AddTennisPlayerApiClient(options =>
			options.BaseAddress = hostContext.Configuration
				.GetSection("ExternalServices:TennisPlayersApi")["Url"]);
		services.AddStatisticsApiClient(options =>
		options.BaseAddress = hostContext.Configuration
				.GetSection("ExternalServices:StatisticsApi")["Url"]);
		services.AddResultProcessing();

		// Services to handle W3 SQS messages
		services.AddSingleton<ISqsMessageChannel, SqsMessageChannel>();  // <<-- wraps the Chanel implementation
		services.AddSingleton<ISqsMessageDeleter, SqsMessageDeleter>();
		services.AddSingleton<ISqsMessageQueue, SqsMessageQueue>();

		// register background service (This ae singleton registrations)
		//services.AddHostedService<QueueReadingService>();
		//services.AddHostedService<ScoreProcessingService>();

		// Swap order to improve Start and Stop event
		services.AddHostedService<ScoreProcessingService>(); // give more time for infligh files
		services.AddHostedService<QueueReadingService>();   // <<-- stop reading que and writing into the channel
				

	})
	.Build();

await host.RunAsync();
