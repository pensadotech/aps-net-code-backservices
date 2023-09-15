using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TennisBookings.ScoreProcessor.Processing;
using TennisBookings.ScoreProcessor.Sqs;
using TennisBookings.ScoreProcessor.BackgroundServices;
using Amazon.SQS.Model;

namespace TennisBookings.ScoreProcessor.Tests;

public class ScoreProcessingServiceTests
{
	[Fact]
	public async Task ShouldStopApplication_WhenExceptionThrown_BecauseServiceProviderDoesNotContainRequiredService()
	{
		// Test atht if a dependency is missing, the application shold stop

		var sp = new ServiceCollection().BuildServiceProvider();

		var hostApplicationLifetime = new Mock<IHostApplicationLifetime>();

		var sqsChannel = new SqsMessageChannel(NullLogger<SqsMessageChannel>.Instance);

		var messages = new Message[]
		{
			new() { MessageId = Guid.NewGuid().ToString() }
		};

		await sqsChannel.WriteMessagesAsync(messages);

		var sut = new ScoreProcessingService(
			NullLogger<ScoreProcessingService>.Instance,
			sqsChannel,
			sp,
			hostApplicationLifetime.Object);

		await sut.StartAsync(default);
		await sut.ExecuteTask;          // <<-- add to make sure that Verify is after the start is completed

		hostApplicationLifetime.Verify(x => x.StopApplication(), Times.Once);
	}

	[Fact]
	public async Task ExecuteAsync_ShouldStopWithoutException_WhenCancelled()
	{
		var sc = new ServiceCollection();
		sc.AddTransient<IScoreProcessor, FakeScoreProcessor>();
		var sp = sc.BuildServiceProvider();

		var sqsChannel = new SqsMessageChannel(NullLogger<SqsMessageChannel>.Instance);

		var sut = new ScoreProcessingService(
			NullLogger<ScoreProcessingService>.Instance,
			sqsChannel,
			sp,
			Mock.Of<IHostApplicationLifetime>());

		await sut.StartAsync(default);

		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
		await sut.StopAsync(cts.Token);
		await sut.ExecuteTask;               // <<-- add to make sure that Verify is after the start is completed

		sut.ExecuteTask.IsCompletedSuccessfully.Should().BeTrue();
	}

	[Fact]
	public async Task ShouldCallScoreProcessor_ForEachMessageInChannel()
	{
		var scoreProcessor = new FakeScoreProcessor();
		var sc = new ServiceCollection();
		sc.AddTransient<IScoreProcessor>(s => scoreProcessor);

		var sp = sc.BuildServiceProvider();

		var sqsChannel = new SqsMessageChannel(NullLogger<SqsMessageChannel>.Instance);

		var messages = new Message[]
		{
			new() { MessageId = Guid.NewGuid().ToString() },
			new() { MessageId = Guid.NewGuid().ToString() }
		};

		await sqsChannel.WriteMessagesAsync(messages);
		sqsChannel.CompleteWriter();                  // <<--- to wait for processing loop to read all mesages

		var sut = new ScoreProcessingService(
			NullLogger<ScoreProcessingService>.Instance,
			sqsChannel,
			sp,
			Mock.Of<IHostApplicationLifetime>());

		await sut.StartAsync(default);
		await sut.ExecuteTask;           // <<-- add to make sure that Verify is after the start is completed

		scoreProcessor.ExecutionCount.Should().Be(2);
	}

	[Fact]
	public async Task ShouldSwallowExceptions_AndStopApplication()
	{
		var sc = new ServiceCollection();

		var scoreProcessor = new Mock<IScoreProcessor>();
		scoreProcessor.Setup(x => x
			.ProcessScoresFromMessageAsync(
				It.IsAny<Message>(),
				It.IsAny<CancellationToken>()))
			.ThrowsAsync(new Exception("My exception"));

		sc.AddTransient(s => scoreProcessor);

		var sp = sc.BuildServiceProvider();

		var hostAppLifetime = new Mock<IHostApplicationLifetime>();

		var sqsChannel = new SqsMessageChannel(NullLogger<SqsMessageChannel>.Instance);

		var messages = new Message[]
		{
			new() { MessageId = Guid.NewGuid().ToString() },
			new() { MessageId = Guid.NewGuid().ToString() }
		};

		await sqsChannel.WriteMessagesAsync(messages);

		var sut = new ScoreProcessingService(
			NullLogger<ScoreProcessingService>.Instance,
			sqsChannel,
			sp,
			hostAppLifetime.Object);

		await sut.StartAsync(default);
		await sut.ExecuteTask;             // <<-- add to make sure that Verify is after the start is completed

		hostAppLifetime.Verify(x => x.StopApplication(), Times.AtLeastOnce);
	}

	private class FakeScoreProcessor : IScoreProcessor
	{
		public int ExecutionCount;

		public Task ProcessScoresFromMessageAsync(Message message,
			CancellationToken cancellationToken = default)
		{
			ExecutionCount++;
			return Task.CompletedTask;
		}
	}
}
