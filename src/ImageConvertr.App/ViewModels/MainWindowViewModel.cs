using System.Diagnostics;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageConvertr.Core.Models;
using ImageConvertr.Core.Services;

namespace ImageConvertr.App.ViewModels;

/// <summary>
/// Represents the view model for the main application window, providing commands and properties for image processing,
/// folder selection, and live status updates.
/// </summary>
/// <remarks>This view model coordinates user interactions and background processing for image conversion
/// workflows. It exposes commands for selecting folders and converting images, and maintains state to enable or disable
/// UI elements appropriately. All properties are designed for data binding in MVVM applications.</remarks>
public partial class MainWindowViewModel : ObservableObject
{
	const int latestActivityUpdateIntervalMilliseconds = 250;

	/// <summary>
	/// The service responsible for converting images to the selected format.
	/// </summary>
	readonly IImageConvertrService imageConvertrService;

	/// <summary>
	/// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
	/// </summary>
	public MainWindowViewModel(IImageConvertrService imageConvertrService)
	{
		this.imageConvertrService = imageConvertrService;
	}

	/// <summary>
	/// The output formats available for image conversion.
	/// </summary>
	public IReadOnlyList<ImageOutputFormatOption> OutputFormats
		=> ImageOutputFormatOption.All;

	/// <summary>
	/// Indicates whether the image processing is currently running.
	/// Used to disable UI elements during the process.
	/// </summary>
	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(SelectImageFolderCommand))]
	[NotifyCanExecuteChangedFor(nameof(SelectOutputFolderCommand))]
	[NotifyCanExecuteChangedFor(nameof(ConvertImagesCommand))]
	[NotifyPropertyChangedFor(nameof(CanEditInputs))]
	[NotifyPropertyChangedFor(nameof(StartButtonText))]
	[NotifyPropertyChangedFor(nameof(ProgressSummary))]
	[NotifyPropertyChangedFor(nameof(ProgressStateText))]
	[NotifyPropertyChangedFor(nameof(ProgressBarValue))]
	bool isProcessing = false;

	/// <summary>
	/// Path to the folder containing image files to process.
	/// </summary>
	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(ConvertImagesCommand))]
	string? imageFolderPath;

	/// <summary>
	/// Path to the folder where output should be saved.
	/// </summary>
	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(ConvertImagesCommand))]
	string? outputFolderPath;

	/// <summary>
	/// The image quality level to use for image processing operations.
	/// </summary>
	[ObservableProperty]
	int imageQuality = 80;

	/// <summary>
	/// The selected target format for converted image files.
	/// </summary>
	[ObservableProperty]
	ImageOutputFormatOption selectedOutputFormat = ImageOutputFormatOption.Webp;

	/// <summary>
	/// Indicates whether existing files should be overwritten during the operation.
	/// </summary>
	[ObservableProperty]
	bool shouldOverwriteExistingFiles = false;

	/// <summary>
	/// The latest process message shown in the live progress card.
	/// </summary>
	[ObservableProperty]
	string latestActivityMessage = "Waiting for conversion activity.";

	/// <summary>
	/// The severity of the latest process message.
	/// </summary>
	[ObservableProperty]
	LogLevel latestActivityLevel = LogLevel.Info;

	/// <summary>
	/// The current one-based image index displayed in the progress area.
	/// </summary>
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(ProgressSummary))]
	[NotifyPropertyChangedFor(nameof(ProgressStateText))]
	[NotifyPropertyChangedFor(nameof(ProgressBarValue))]
	int progressValue = 0;

	/// <summary>
	/// The total number of supported images in the current batch.
	/// </summary>
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(ProgressSummary))]
	[NotifyPropertyChangedFor(nameof(ProgressStateText))]
	[NotifyPropertyChangedFor(nameof(ProgressBarMaximum))]
	int progressMaximum = 0;

	/// <summary>
	/// Indicates whether inputs can be changed.
	/// </summary>
	public bool CanEditInputs => !IsProcessing;

	/// <summary>
	/// Gets the start button text for the current state.
	/// </summary>
	public string StartButtonText => IsProcessing ? "Converting images..." : "Convert images";

	/// <summary>
	/// Gets the progress headline for the live progress card.
	/// </summary>
	public string ProgressSummary =>
		$"{ProgressValue.ToString(CultureInfo.CurrentCulture)} / {ProgressMaximum.ToString(CultureInfo.CurrentCulture)} images";

	/// <summary>
	/// Gets the progress state text for the live progress card.
	/// </summary>
	public string ProgressStateText => ProgressMaximum <= 0
		? "0%"
		: $"{Math.Round((double)ProgressValue / ProgressMaximum * 100,
			MidpointRounding.AwayFromZero).ToString(CultureInfo.InvariantCulture)}%";

	/// <summary>
	/// Gets a determinate fallback value when no conversion is running.
	/// </summary>
	public int ProgressBarValue => Math.Min(ProgressValue, ProgressBarMaximum);

	/// <summary>
	/// Gets the safe progress bar maximum value.
	/// </summary>
	public int ProgressBarMaximum => Math.Max(1, ProgressMaximum);

	/// <summary>
	/// Gets or sets the delegate used to pick the input image folder.
	/// </summary>
	public Func<Task<string?>>? PickImageFolderDelegate { get; set; }

	/// <summary>
	/// Gets or sets the delegate used to pick the output folder.
	/// </summary>
	public Func<Task<string?>>? PickOutputFolderDelegate { get; set; }

	/// <summary>
	/// Opens a folder picker dialog for selecting the input image folder.
	/// </summary>
	[RelayCommand(
		AllowConcurrentExecutions = false,
		CanExecute = nameof(CanSelectFolder))]
	async Task SelectImageFolder()
	{
		if (PickImageFolderDelegate is null)
		{
			throw new InvalidOperationException("No folder-pick delegate assigned. The view must assign PickImageFolderDelegate.");
		}

		ImageFolderPath = await PickImageFolderDelegate() ?? string.Empty;

		if (string.IsNullOrEmpty(ImageFolderPath))
		{
			LatestActivityMessage = "No Image Folder selected.";
			LatestActivityLevel = LogLevel.Error;
		}
		else
		{
			LatestActivityMessage = "Choose an output folder and conversion settings.";
			LatestActivityLevel = LogLevel.Info;
		}
	}

	/// <summary>
	/// Opens a folder picker dialog for selecting the output folder.
	/// </summary>
	[RelayCommand(
		AllowConcurrentExecutions = false,
		CanExecute = nameof(CanSelectFolder))]
	async Task SelectOutputFolder()
	{
		if (PickOutputFolderDelegate is null)
		{
			throw new InvalidOperationException("No folder-pick delegate assigned. The view must assign PickOutputFolderDelegate.");
		}

		OutputFolderPath = await PickOutputFolderDelegate() ?? string.Empty;

		if (string.IsNullOrEmpty(OutputFolderPath))
		{
			LatestActivityMessage = "No Output Folder selected.";
			LatestActivityLevel = LogLevel.Error;
		}
		else
		{
			LatestActivityMessage = "Ready to convert when both folders are set.";
			LatestActivityLevel = LogLevel.Info;
		}
	}

	/// <summary>
	/// Main command to start the image conversion process. 
	/// It runs the conversion on a background thread to keep the UI responsive,
	/// </summary>
	[RelayCommand(
		AllowConcurrentExecutions = false,
		CanExecute = nameof(CanProcessImages))]
	async Task ConvertImagesAsync()
	{
		IsProcessing = true;
		ProgressValue = 0;
		ProgressMaximum = 0;
		LatestActivityMessage = "Scanning the selected folder for supported images.";
		LatestActivityLevel = LogLevel.Info;
		Stopwatch latestActivityStopwatch = Stopwatch.StartNew();
		int conversionFinished = 0;

		Progress<ImageConversionProgressUpdate> progress = new(update =>
		{
			if (Volatile.Read(ref conversionFinished) == 1)
			{
				return;
			}

			ProgressMaximum = update.TotalCount;
			ProgressValue = Math.Clamp(update.CurrentIndex, 0, Math.Max(0, update.TotalCount));

			if (update.Level == LogLevel.Error
				|| latestActivityStopwatch.ElapsedMilliseconds >= latestActivityUpdateIntervalMilliseconds)
			{
				LatestActivityMessage = update.Message;
				LatestActivityLevel = update.Level;
				latestActivityStopwatch.Restart();
			}
		});

		void ApplyConversionError(Exception ex)
		{
			Volatile.Write(ref conversionFinished, 1);
			LatestActivityMessage = ex.Message;
			LatestActivityLevel = LogLevel.Error;
		}

		try
		{
			ImageConvertResult result = await Task.Run(() =>
				imageConvertrService.ConvertFolder(
					ImageFolderPath!,
					OutputFolderPath!,
					SelectedOutputFormat,
					ImageQuality,
					ShouldOverwriteExistingFiles,
					progress));
			Volatile.Write(ref conversionFinished, 1);

			if (ProgressMaximum == 0)
			{
				ProgressValue = 0;
			}
			else
			{
				ProgressValue = ProgressMaximum;
			}

			LatestActivityMessage = FormatCompletionSummary(result);
			LatestActivityLevel = result.Failed > 0 ? LogLevel.Error : LogLevel.Success;
		}
		catch (ArgumentException ex)
		{
			ApplyConversionError(ex);
		}
		catch (IOException ex)
		{
			ApplyConversionError(ex);
		}
		catch (UnauthorizedAccessException ex)
		{
			ApplyConversionError(ex);
		}
		catch (InvalidOperationException ex)
		{
			ApplyConversionError(ex);
		}
		catch (NotSupportedException ex)
		{
			ApplyConversionError(ex);
		}
		finally
		{
			IsProcessing = false;
		}
	}

	/// <summary>
	/// Determines whether folder selection commands can be executed.
	/// </summary>
	bool CanSelectFolder()
		=> !IsProcessing;

	/// <summary>
	/// Determines whether the image converting command can be executed.
	/// </summary>
	bool CanProcessImages()
	{
		return !IsProcessing 
			&& !string.IsNullOrEmpty(ImageFolderPath)
			&& !string.IsNullOrEmpty(OutputFolderPath);
	}

	/// <summary>
	/// Formats the final completion summary for a conversion operation.
	/// </summary>
	static string FormatCompletionSummary(ImageConvertResult result)
	{
		int totalImages = result.Processed + result.Skipped + result.Failed;
		string imageText = totalImages == 1
			? "1 image"
			: $"{totalImages.ToString(CultureInfo.CurrentCulture)} images";

		return $"{imageText} processed. {result.SavedMB.ToString("F2", CultureInfo.CurrentCulture)} MB saved.";
	}

}
