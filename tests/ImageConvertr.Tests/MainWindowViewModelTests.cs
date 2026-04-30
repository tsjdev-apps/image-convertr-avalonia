using ImageConvertr.App.ViewModels;
using ImageConvertr.Core.Models;
using ImageConvertr.Core.Services;

namespace ImageConvertr.Tests;

/// <summary>
/// Verifies the main window view model behavior.
/// </summary>
public sealed class MainWindowViewModelTests
{
	static readonly TimeSpan conditionWaitTimeout = TimeSpan.FromSeconds(5);

	/// <summary>
	/// Verifies that a new conversion run clears the previous completion summary immediately.
	/// </summary>
	[Fact]
	public async Task ConvertImagesCommandClearsLatestActivityWhenANewRunStarts()
	{
		ControlledProgressImageConvertrService service = new(new ImageConvertResult(3, 0, 0, 1));
		MainWindowViewModel viewModel = CreateReadyViewModel(service);
		viewModel.LatestActivityMessage = "3 images processed. 1.00 MB saved.";
		viewModel.LatestActivityLevel = LogLevel.Success;

		Task startTask = viewModel.ConvertImagesCommand.ExecuteAsync(null);

		await service.WaitUntilStartedAsync(TestContext.Current.CancellationToken);

		Assert.True(viewModel.IsProcessing);
		Assert.False(viewModel.CanEditInputs);
		Assert.Equal("Converting images...", viewModel.StartButtonText);
		Assert.Equal("Scanning the selected folder for supported images.", viewModel.LatestActivityMessage);
		Assert.Equal(LogLevel.Info, viewModel.LatestActivityLevel);
		Assert.Equal(0, viewModel.ProgressValue);
		Assert.Equal(0, viewModel.ProgressMaximum);
		Assert.Equal("0%", viewModel.ProgressStateText);

		service.Complete();
		await startTask;
	}

	/// <summary>
	/// Verifies that error progress updates reach the view model while the operation is still running.
	/// </summary>
	[Fact]
	public async Task ConvertImagesCommandAppliesErrorProgressBeforeServiceCompletes()
	{
		ControlledProgressImageConvertrService service = new(new ImageConvertResult(0, 0, 1, 0));
		MainWindowViewModel viewModel = CreateReadyViewModel(service);

		Task startTask = viewModel.ConvertImagesCommand.ExecuteAsync(null);

		await service.WaitUntilStartedAsync(TestContext.Current.CancellationToken);

		const string progressMessage = "Failed to convert 'broken.jpg' because it could not be decoded.";
		service.ReportProgress(new ImageConversionProgressUpdate(
			CurrentIndex: 1,
			TotalCount: 3,
			Message: progressMessage,
			Level: LogLevel.Error));

		await WaitForConditionAsync(
			() => viewModel.ProgressValue == 1
				&& viewModel.ProgressMaximum == 3
				&& viewModel.LatestActivityMessage == progressMessage
				&& viewModel.LatestActivityLevel == LogLevel.Error,
			"Timed out waiting for the reported progress to update the view model state.",
			TestContext.Current.CancellationToken);

		Assert.True(viewModel.IsProcessing);
		Assert.False(startTask.IsCompleted);

		service.Complete();
		await startTask;
	}

	/// <summary>
	/// Verifies that late progress reports cannot replace the completion summary.
	/// </summary>
	[Fact]
	public async Task ConvertImagesCommandKeepsCompletionSummaryWhenProgressArrivesAfterServiceCompletes()
	{
		LateProgressImageConvertrService service = new();
		MainWindowViewModel viewModel = CreateReadyViewModel(service);

		await viewModel.ConvertImagesCommand.ExecuteAsync(null);

		const string expectedSummary = "3 images processed. 1.00 MB saved.";
		Assert.Equal(expectedSummary, viewModel.LatestActivityMessage);
		Assert.Equal(LogLevel.Success, viewModel.LatestActivityLevel);

		await service.ReportLateProgressAsync(TestContext.Current.CancellationToken);

		Assert.Equal(expectedSummary, viewModel.LatestActivityMessage);
		Assert.Equal(LogLevel.Success, viewModel.LatestActivityLevel);
	}

	/// <summary>
	/// Verifies that the image folder picker updates the validation state when the user cancels it.
	/// </summary>
	[Fact]
	public async Task SelectImageFolderCommandShowsErrorWhenPickerIsCanceled()
	{
		MainWindowViewModel viewModel = new(new ImmediateImageConvertrService())
		{
			PickImageFolderDelegate = () => Task.FromResult<string?>(null)
		};

		await viewModel.SelectImageFolderCommand.ExecuteAsync(null);

		Assert.Equal(string.Empty, viewModel.ImageFolderPath);
		Assert.Equal("No Image Folder selected.", viewModel.LatestActivityMessage);
		Assert.Equal(LogLevel.Error, viewModel.LatestActivityLevel);
	}

	/// <summary>
	/// Verifies that the output folder picker updates the ready state when a folder is selected.
	/// </summary>
	[Fact]
	public async Task SelectOutputFolderCommandShowsReadyMessageWhenFolderIsSelected()
	{
		MainWindowViewModel viewModel = new(new ImmediateImageConvertrService())
		{
			PickOutputFolderDelegate = () => Task.FromResult<string?>("output")
		};

		await viewModel.SelectOutputFolderCommand.ExecuteAsync(null);

		Assert.Equal("output", viewModel.OutputFolderPath);
		Assert.Equal("Ready to convert when both folders are set.", viewModel.LatestActivityMessage);
		Assert.Equal(LogLevel.Info, viewModel.LatestActivityLevel);
	}

	/// <summary>
	/// Creates a view model with valid conversion input and output folder paths.
	/// </summary>
	static MainWindowViewModel CreateReadyViewModel(IImageConvertrService service)
	{
		return new MainWindowViewModel(service)
		{
			ImageFolderPath = "input",
			OutputFolderPath = "output"
		};
	}

	/// <summary>
	/// Waits until a test condition becomes true.
	/// </summary>
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

	/// <summary>
	/// Provides a test ImageConvertr service that can be held open while progress is reported.
	/// </summary>
	sealed class ControlledProgressImageConvertrService(ImageConvertResult result) : IImageConvertrService
	{
		readonly TaskCompletionSource started = new(TaskCreationOptions.RunContinuationsAsynchronously);
		readonly TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
		IProgress<ImageConversionProgressUpdate>? capturedProgress;

		/// <summary>
		/// Waits until the conversion operation has started.
		/// </summary>
		public Task WaitUntilStartedAsync(CancellationToken cancellationToken)
		{
			return started.Task.WaitAsync(cancellationToken);
		}

		/// <summary>
		/// Reports progress while the conversion operation is still running.
		/// </summary>
		public void ReportProgress(ImageConversionProgressUpdate update)
		{
			if (capturedProgress is null)
			{
				throw new InvalidOperationException("The conversion operation has not started yet.");
			}

			capturedProgress.Report(update);
		}

		/// <summary>
		/// Completes the conversion operation.
		/// </summary>
		public void Complete()
		{
			completion.TrySetResult();
		}

		/// <inheritdoc/>
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

	/// <summary>
	/// Provides a test ImageConvertr service that reports progress after completing.
	/// </summary>
	sealed class LateProgressImageConvertrService : IImageConvertrService
	{
		readonly TaskCompletionSource lateProgressHandled = new(TaskCreationOptions.RunContinuationsAsynchronously);
		IProgress<ImageConversionProgressUpdate>? capturedProgress;

		/// <summary>
		/// Reports progress after the conversion operation has already completed and waits for delivery.
		/// </summary>
		public Task ReportLateProgressAsync(CancellationToken cancellationToken)
		{
			if (capturedProgress is null)
			{
				throw new InvalidOperationException("The conversion operation has not started yet.");
			}

			capturedProgress.Report(new ImageConversionProgressUpdate(
				CurrentIndex: 3,
				TotalCount: 3,
				Message: "Converted 'late.jpg' to PNG.",
				Level: LogLevel.Success));

			return lateProgressHandled.Task.WaitAsync(cancellationToken);
		}

		/// <inheritdoc/>
		public ImageConvertResult ConvertFolder(
			string imageInputPath,
			string imageOutputPath,
			ImageOutputFormatOption outputFormat,
			int imageQuality = 85,
			bool overwriteExisting = false,
			IProgress<ImageConversionProgressUpdate>? progress = null)
		{
			capturedProgress = progress
				?? throw new InvalidOperationException("The test requires a progress reporter.");

			if (capturedProgress is not Progress<ImageConversionProgressUpdate> concreteProgress)
			{
				throw new InvalidOperationException("The test requires Progress<ImageConversionProgressUpdate> to observe late progress delivery.");
			}

			concreteProgress.ProgressChanged += HandleProgressChanged;

			return new ImageConvertResult(Processed: 2, Skipped: 1, Failed: 0, SavedMB: 1);
		}

		void HandleProgressChanged(object? sender, ImageConversionProgressUpdate update)
		{
			if (sender is Progress<ImageConversionProgressUpdate> concreteProgress)
			{
				concreteProgress.ProgressChanged -= HandleProgressChanged;
			}

			lateProgressHandled.TrySetResult();
		}
	}

	/// <summary>
	/// Provides an immediate no-op conversion service for folder selection tests.
	/// </summary>
	sealed class ImmediateImageConvertrService : IImageConvertrService
	{
		/// <inheritdoc/>
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
