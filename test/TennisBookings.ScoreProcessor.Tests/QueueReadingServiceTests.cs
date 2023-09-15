using Amazon.SQS.Model;
using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TennisBookings.ScoreProcessor.BackgroundServices;
using TennisBookings.ScoreProcessor.Sqs;

namespace TennisBookings.ScoreProcessor.Tests;

public class QueueReadingServiceTests
{
	[Fact]
	public async Task ShouldSwallowExceptions_AndCompleteWriter()
	{
		// Arrange

		var sqsChannel = new Mock<ISqsMessageChannel>();

		var sqsMessageQueue = new Mock<ISqsMessageQueue>();
		sqsMessageQueue.Setup(x => x
			.ReceiveMessageAsync(
				It.IsAny<ReceiveMessageRequest>(),     //<< -- anny call received
				It.IsAny<CancellationToken>()))
			.ThrowsAsync(new Exception("My exception"));  // <<-- for every messagem respond with exception

		using var sut = new QueueReadingService(           // <<--recevied the injected parameters
			NullLogger<QueueReadingService>.Instance,
			sqsMessageQueue.Object,
			Options.Create(new AwsServicesConfiguration
			{
				ScoresQueueUrl = "https://www.example.com"
			}),
			sqsChannel.Object);

		// Act

		await sut.StartAsync(default);   // <<- initiate teh worker service

		// Assert

		sqsChannel.Verify(x => x.TryCompleteWriter(null), Times.Once);   // <<-- verify that try completed was executed one time
	}

	[Fact]
	public async Task ShouldStopWithoutException_WhenCancelled()   // <<-- verify that the services shutsdown with no excpetions
	{
		var sqsChannel = new SqsMessageChannel(NullLogger<SqsMessageChannel>.Instance);

		var sqsMessageQueue = new Mock<ISqsMessageQueue>();
		sqsMessageQueue.Setup(x => x
			.ReceiveMessageAsync(
				It.IsAny<ReceiveMessageRequest>(),
				It.IsAny<CancellationToken>()))
			.ReturnsAsync(new ReceiveMessageResponse          // <<-- respond with an Ok
			{
				HttpStatusCode = System.Net.HttpStatusCode.OK,
				Messages = new List<Message>()
			});

		var sut = new QueueReadingService(
			NullLogger<QueueReadingService>.Instance,
			sqsMessageQueue.Object,
			Options.Create(new AwsServicesConfiguration
			{
				ScoresQueueUrl = "https://www.example.com"
			}),
			sqsChannel);

		// Act
		await sut.StartAsync(default);

		// Create cancalation token
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));

		Func<Task> act = async () => { await sut.StopAsync(cts.Token); };

		// Assert
		await act.Should().NotThrowAsync();   // <<-- validate that shutdown was ok
	}
}
