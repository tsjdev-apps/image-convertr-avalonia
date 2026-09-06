using ImageConvertr.App.Resources.Localization;
using ImageConvertr.App.ViewModels;
using ImageConvertr.Core.Models;
using ImageConvertr.Core.Services;

namespace ImageConvertr.Tests;

#pragma warning disable CA1863 // The localized format is intentionally resolved at runtime.

/// <summary>
/// Verifies the main window view model behavior.
/// </summary>
public sealed class MainWindowViewModelTests
{
	static readonly TimeSpan conditionWaitTimeout = TimeSpan.FromSeconds(5);

	/// <summary>
	/// Verifies that starting a new conversion clears all state from the previous run.
	/// </summary>
	[Fact]
	public async Task ConvertImagesCommandClearsHistoryWhenANewRunStarts()
	{
		ControlledImageConvertrService service = new(new ImageConvertResult(3, 0, 0, 1));
		MainWindowViewModel viewModel = CreateReadyViewModel(service);
		viewModel.ConversionHistory.Add(
			ConversionHistoryEntry.CreateSummary(new ImageConvertResult(1, 0, 0, 0)));
		viewModel.HasConversionHistory = true;

		Task startTask = viewModel.ConvertImagesCommand.ExecuteAsync(null);
		await service.WaitUntilStartedAsync(TestContext.Current.CancellationToken);

		Assert.True(viewModel.IsProcessing);
		Assert.False(viewModel.CanEditInputs);
		Assert.Equal(Strings.ConvertingImages_Button, viewModel.StartButtonText);
		Assert.Empty(viewModel.ConversionHistory);
		Assert.True(viewModel.IsConversionHistoryEmpty);
		Assert.Equal(0, viewModel.ProgressValue);
		Assert.Equal(0, viewModel.ProgressMaximum);
		Assert.Equal(0, viewModel.ProgressPercentage);

		service.Complete();
		await startTask;
	}

	/// <summary>
	/// Verifies that each completed file updates progress and creates a structured history row.
	/// </summary>
	[Fact]
	public async Task ConvertImagesCommandAddsProgressEntryBeforeServiceCompletes()
	{
		ControlledImageConvertrService service = new(new ImageConvertResult(1, 0, 0, 0.5));
		MainWindowViewModel viewModel = CreateReadyViewModel(service);
		Task startTask = viewModel.ConvertImagesCommand.ExecuteAsync(null);
		await service.WaitUntilStartedAsync(TestContext.Current.CancellationToken);

		service.ReportProgress(CreateUpdate(
			processedCount: 1,
			totalCount: 3,
			ConversionStatus.Converted,
			"camera.png",
			"camera.webp"));

		await WaitForConditionAsync(
			() => viewModel.ProgressValue == 1
				&& viewModel.ProgressMaximum == 3
				&& viewModel.ConversionHistory.Count == 1,
			"Timed out waiting for the progress row.",
			TestContext.Current.CancellationToken);

		ConversionHistoryEntry entry = Assert.Single(viewModel.ConversionHistory);
		Assert.Equal("camera.png", entry.SourceFileName);
		Assert.Equal(ConversionStatus.Converted, entry.Status);
		Assert.Contains("camera.webp", entry.ResultText, StringComparison.Ordinal);
		Assert.Equal(33, viewModel.ProgressPercentage);
		Assert.False(startTask.IsCompleted);

		service.Complete();
		await startTask;
	}

	/// <summary>
	/// Verifies that success, skip, and failure entries are followed by a final summary row.
	/// </summary>
	[Fact]
	public async Task ConvertImagesCommandAddsAllStatusesAndCompletionSummary()
	{
		ControlledImageConvertrService service = new(new ImageConvertResult(1, 1, 1, 0.25));
		MainWindowViewModel viewModel = CreateReadyViewModel(service);
		Task startTask = viewModel.ConvertImagesCommand.ExecuteAsync(null);
		await service.WaitUntilStartedAsync(TestContext.Current.CancellationToken);

		service.ReportProgress(CreateUpdate(1, 3, ConversionStatus.Converted, "one.png", "one.webp"));
		service.ReportProgress(CreateUpdate(
			2,
			3,
			ConversionStatus.Skipped,
			"two.jpg",
			"two.webp",
			ConversionErrorKind.TargetExists));
		service.ReportProgress(CreateUpdate(
			3,
			3,
			ConversionStatus.Failed,
			"three.gif",
			"three.webp",
			ConversionErrorKind.DecodeFailed));

		service.Complete();
		await startTask;

		Assert.Equal(4, viewModel.ConversionHistory.Count);
		Assert.Equal(ConversionStatus.Converted, viewModel.ConversionHistory[0].Status);
		Assert.Equal(ConversionStatus.Skipped, viewModel.ConversionHistory[1].Status);
		Assert.Equal(ConversionStatus.Failed, viewModel.ConversionHistory[2].Status);
		Assert.True(viewModel.ConversionHistory[3].IsSummary);
		Assert.Contains(Strings.Summary_Title, viewModel.ConversionHistory[3].SourceFileName, StringComparison.Ordinal);
		Assert.Equal(3, viewModel.ProgressValue);
		Assert.Equal(3, viewModel.ProgressMaximum);
		Assert.Equal(100, viewModel.ProgressPercentage);
	}

	/// <summary>
	/// Verifies that progress reported after completion cannot append after the summary.
	/// </summary>
	[Fact]
	public async Task ConvertImagesCommandIgnoresProgressAfterCompletion()
	{
		LateProgressImageConvertrService service = new();
		MainWindowViewModel viewModel = CreateReadyViewModel(service);

		await viewModel.ConvertImagesCommand.ExecuteAsync(null);
		Assert.Single(viewModel.ConversionHistory);
		Assert.True(viewModel.ConversionHistory[0].IsSummary);

		service.ReportLateProgress();

		Assert.Single(viewModel.ConversionHistory);
		Assert.True(viewModel.ConversionHistory[0].IsSummary);
	}

	/// <summary>
	/// Verifies localized validation when the source picker is canceled.
	/// </summary>
	[Fact]
	public async Task SelectImageFolderCommandShowsLocalizedValidationWhenCanceled()
	{
		MainWindowViewModel viewModel = new(new ImmediateImageConvertrService())
		{
			PickImageFolderDelegate = () => Task.FromResult<string?>(null)
		};

		await viewModel.SelectImageFolderCommand.ExecuteAsync(null);

		Assert.Equal(string.Empty, viewModel.ImageFolderPath);
		Assert.Equal(Strings.Validation_SourceFolderRequired, viewModel.ValidationMessage);
		Assert.True(viewModel.HasValidationMessage);
	}

	/// <summary>
	/// Verifies that selecting a target folder clears previous validation.
	/// </summary>
	[Fact]
	public async Task SelectOutputFolderCommandClearsValidationWhenFolderIsSelected()
	{
		MainWindowViewModel viewModel = new(new ImmediateImageConvertrService())
		{
			ValidationMessage = "previous",
			PickOutputFolderDelegate = () => Task.FromResult<string?>("output")
		};

		await viewModel.SelectOutputFolderCommand.ExecuteAsync(null);

		Assert.Equal("output", viewModel.OutputFolderPath);
		Assert.Null(viewModel.ValidationMessage);
		Assert.False(viewModel.HasValidationMessage);
	}

	/// <summary>
	/// Verifies singular and plural processed-file formatting.
	/// </summary>
	[Fact]
	public void ProgressSummaryUsesSingularAndPluralResources()
	{
		MainWindowViewModel viewModel = new(new ImmediateImageConvertrService())
		{
			ProgressValue = 1,
			ProgressMaximum = 1
		};

		Assert.Equal(
			string.Format(
				System.Globalization.CultureInfo.CurrentCulture,
				Strings.Progress_ProcessedFile,
				1,
				1),
			viewModel.ProgressSummary);

		viewModel.ProgressMaximum = 2;

		Assert.Equal(
			string.Format(
				System.Globalization.CultureInfo.CurrentCulture,
				Strings.Progress_ProcessedFiles,
				1,
				2),
			viewModel.ProgressSummary);
	}

	static ImageConversionProgressUpdate CreateUpdate(
		int processedCount,
		int totalCount,
		ConversionStatus status,
		string sourceFileName,
		string targetFileName,
		ConversionErrorKind errorKind = ConversionErrorKind.None)
	{
		return new ImageConversionProgressUpdate(
			processedCount,
			totalCount,
			sourceFileName,
			targetFileName,
			Path.GetExtension(sourceFileName).TrimStart('.').ToUpperInvariant(),
			"WEBP",
			status,
			SourceFileSize: 2048,
			TargetFileSize: status is ConversionStatus.Converted or ConversionStatus.Overwritten ? 1024 : null,
			errorKind);
	}

	static MainWindowViewModel CreateReadyViewModel(IImageConvertrService service)
	{
		return new MainWindowViewModel(service)
		{
			ImageFolderPath = "input",
			OutputFolderPath = "output"
		};
	}

	static async Task WaitForConditionAsync(
		Func<bool> condition,
		string failureMessage,
		CancellationToken cancellationToken)
	{
		if (condition())
		{
			return;
		}

		using CancellationTokenSource timeoutSource = new(conditionWaitTimeout);
		using CancellationTokenSource linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
			cancellationToken,
			timeoutSource.Token);

		while (!condition())
		{
			try
			{
				await Task.Delay(10, linkedCancellation.Token);
			}
			catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested)
			{
				throw new TimeoutException(failureMessage);
			}
		}
	}

	sealed class ControlledImageConvertrService(ImageConvertResult result) : IImageConvertrService
	{
		readonly TaskCompletionSource started = new(TaskCreationOptions.RunContinuationsAsynchronously);
		readonly TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
		IProgress<ImageConversionProgressUpdate>? capturedProgress;

		public Task WaitUntilStartedAsync(CancellationToken cancellationToken)
			=> started.Task.WaitAsync(cancellationToken);

		public void ReportProgress(ImageConversionProgressUpdate update)
		{
			(capturedProgress
				?? throw new InvalidOperationException("The conversion operation has not started yet."))
				.Report(update);
		}

		public void Complete() => completion.TrySetResult();

		public ImageConvertResult ConvertFolder(
			string imageInputPath,
			string imageOutputPath,
			ImageOutputFormatOption outputFormat,
			int imageQuality = 85,
			bool overwriteExisting = false,
			IProgress<ImageConversionProgressUpdate>? progress = null)
		{
			capturedProgress = progress;
			started.TrySetResult();
			completion.Task.GetAwaiter().GetResult();
			return result;
		}
	}

	sealed class LateProgressImageConvertrService : IImageConvertrService
	{
		IProgress<ImageConversionProgressUpdate>? capturedProgress;

		public void ReportLateProgress()
		{
			(capturedProgress
				?? throw new InvalidOperationException("The conversion operation has not started yet."))
				.Report(CreateUpdate(1, 1, ConversionStatus.Converted, "late.png", "late.webp"));
		}

		public ImageConvertResult ConvertFolder(
			string imageInputPath,
			string imageOutputPath,
			ImageOutputFormatOption outputFormat,
			int imageQuality = 85,
			bool overwriteExisting = false,
			IProgress<ImageConversionProgressUpdate>? progress = null)
		{
			capturedProgress = progress;
			return new ImageConvertResult(0, 0, 0, 0);
		}
	}

	sealed class ImmediateImageConvertrService : IImageConvertrService
	{
		public ImageConvertResult ConvertFolder(
			string imageInputPath,
			string imageOutputPath,
			ImageOutputFormatOption outputFormat,
			int imageQuality = 85,
			bool overwriteExisting = false,
			IProgress<ImageConversionProgressUpdate>? progress = null)
		{
			return new ImageConvertResult(0, 0, 0, 0);
		}
	}
}

#pragma warning restore CA1863
