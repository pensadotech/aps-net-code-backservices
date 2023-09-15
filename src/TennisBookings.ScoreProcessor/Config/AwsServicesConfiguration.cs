namespace TennisBookings.ScoreProcessor;

public class AwsServicesConfiguration
{
	public string ScoresQueueUrl { get; set; } = string.Empty;
	public string LocalstackScoresQueueUrl { get; set; } = string.Empty;
	public string ScoresBucketName { get; set; } = string.Empty;
	public bool UseLocalStack { get; set; }
}
