using System.Diagnostics;
using Amazon.S3;
using Amazon.S3.Transfer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TennisBookings.Processing;
using TennisBookings.ResultsProcessing;

namespace TennisBookings.Areas.Admin.Controllers;

[Area("Admin")]
[Route("Admin/[controller]")]
[Authorize(Roles = "Admin")]
public class ResultsController : Controller
{
	private readonly IResultProcessor _resultProcessor;
	private readonly ILogger<ResultsController> _logger;
	private readonly FileProcessingChannel _fileProcessingChannel;
	private readonly IAmazonS3 _amazonS3;
	private readonly string _s3BucketName;

	public ResultsController(
		IResultProcessor resultProcessor,
		ILogger<ResultsController> logger,
		FileProcessingChannel fileProcessingChannel,
		IAmazonS3 amazonS3,
		IOptions<ScoreProcesingConfiguration> options)
	{
		_resultProcessor = resultProcessor;
		_logger = logger;
		_fileProcessingChannel = fileProcessingChannel;
		_amazonS3 = amazonS3;
		_s3BucketName = options.Value.S3BucketName;
	}

	[HttpGet]
	public IActionResult UploadResults()
	{
		return View();
	}

	[HttpGet("v2")]
	public IActionResult UploadResultsV2()
	{
		return View();
	}

	[HttpGet("v3")]
	public IActionResult UploadResultsV3()
	{
		return View();
	}

	[HttpPost("FileUpload")]
	public async Task<IActionResult> FileUpload(IFormFile file, CancellationToken cancellationToken)
	{
		var sw = Stopwatch.StartNew();

		if (file is object && file.Length > 0)
		{
			var fileName = Path.GetTempFileName(); // Upload to a temp file path

			await using var stream = new FileStream(fileName, FileMode.Create);

			await file.CopyToAsync(stream, cancellationToken);

			stream.Position = 0;

			await _resultProcessor.ProcessAsync(stream, cancellationToken);

			System.IO.File.Delete(fileName); // Delete the temp file
		}

		sw.Stop();

		_logger.LogInformation("Time taken for result upload and processing " +
			"was {ElapsedMilliseconds}ms.", sw.ElapsedMilliseconds);

		return RedirectToAction("UploadComplete");
	}

	[HttpPost("FileUploadV2")]
	public async Task<IActionResult> FileUploadV2(IFormFile file, CancellationToken cancellationToken)
	{
		var sw = Stopwatch.StartNew();

		// Verify if data is available to upload in teh incoming file
		if (file is object && file.Length > 0)
		{
			// Get a temporary filename
			var fileName = Path.GetTempFileName();

			// Copy file into the server using a file stream
			using (var stream = new FileStream(fileName, FileMode.Create,
				FileAccess.Write))
			{
				// Copy incoming file into the temporary location defined by the stream
				await file.CopyToAsync(stream, cancellationToken);
			}

			// define a 3 sec wait time before acepting teh cancelation token
			// to account for teh channel delays and capacity to accept new files
			using var cts = CancellationTokenSource
				.CreateLinkedTokenSource(cancellationToken);
			cts.CancelAfter(TimeSpan.FromSeconds(3)); // wait max 3 seconds

			try
			{
				// write the file into the channel 
				var fileWritten = await _fileProcessingChannel
					.AddFileAsync(fileName, cts.Token);

				if (fileWritten)
				{
					sw.Stop();

					_logger.LogInformation("Time for result upload " +
						"was {ElapsedMilliseconds}ms.", sw.ElapsedMilliseconds);

					// After uploading the file to teh channel,
					// redirect to the screen to infromat the user
					return RedirectToAction("UploadComplete");
				}
			}
			catch (OperationCanceledException) when (cts.IsCancellationRequested)
			{
				// Considration to delete teh temp file in case of an error or
				// cancelation action
				System.IO.File.Delete(fileName); // Delete the temp file to cleanup
			}
		}

		sw.Stop();

		_logger.LogInformation("Time taken for result upload and processing " +
			"was {ElapsedMilliseconds}ms.", sw.ElapsedMilliseconds);

		return RedirectToAction("UploadFailed");
	}

	[HttpPost("FileUploadV3")]
	public async Task<IActionResult> FileUploadV3(IFormFile file, CancellationToken cancellationToken)
	{
		var sw = Stopwatch.StartNew();

		if (file is object && file.Length > 0)
		{
			// instead of saving the file locally, it will be stored through S3

			// Generate a unique key for teh object to upload
			// S3 identify files within the bucket using the key 
			var objectKey = $"{Guid.NewGuid()}.csv";

			try
			{
				// Upload file to S3. From here the background services created
				// will process the file by reading it out of the S3 queue
				using var fileTransferUtility = new TransferUtility(_amazonS3);

				await using var uploadedFileStream = file.OpenReadStream();

				await fileTransferUtility
					.UploadAsync(uploadedFileStream, _s3BucketName,
						objectKey, cancellationToken);
			}
			catch (Exception)
			{
				return RedirectToAction("UploadFailed");
			}
		}

		sw.Stop();

		_logger.LogInformation("Time taken for result upload and processing " +
			"was {ElapsedMilliseconds}ms.", sw.ElapsedMilliseconds);

		return RedirectToAction("UploadComplete");
	}

	[HttpGet("FileUploadComplete")]
	public IActionResult UploadComplete()
	{
		return View();
	}

	[HttpGet("FileUploadFailed")]
	public IActionResult UploadFailed()
	{
		return View();
	}
}
