using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageConvertr.App.Resources.Localization;
using ImageConvertr.Core.Models;
using ImageConvertr.Core.Services;

namespace ImageConvertr.App.ViewModels;

/// <summary>
/// Coordinates folder selection, image conversion, validation, and live conversion history.
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
	readonly IImageConvertrService imageConvertrService;

	/// <summary>
	/// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
	/// </summary>
	public MainWindowViewModel(IImageConvertrService imageConvertrService)
	{
		this.imageConvertrService = imageConvertrService;
	}

	/// <summary>Gets the output formats available for image conversion.</summary>
	public IReadOnlyList<ImageOutputFormatOption> OutputFormats => ImageOutputFormatOption.All;

	/// <summary>Gets the entries produced during the current conversion operation.</summary>
	public ObservableCollection<ConversionHistoryEntry> ConversionHistory { get; } = [];

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(SelectImageFolderCommand))]
	[NotifyCanExecuteChangedFor(nameof(SelectOutputFolderCommand))]
	[NotifyCanExecuteChangedFor(nameof(ConvertImagesCommand))]
	[NotifyPropertyChangedFor(nameof(CanEditInputs))]
	[NotifyPropertyChangedFor(nameof(StartButtonText))]
	bool isProcessing;

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(ConvertImagesCommand))]
	string? imageFolderPath;

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(ConvertImagesCommand))]
	string? outputFolderPath;

	[ObservableProperty]
	int imageQuality = 80;

	[ObservableProperty]
	ImageOutputFormatOption selectedOutputFormat = ImageOutputFormatOption.Webp;

	[ObservableProperty]
	bool shouldOverwriteExistingFiles;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(ProgressSummary))]
	[NotifyPropertyChangedFor(nameof(ProgressPercentage))]
	[NotifyPropertyChangedFor(nameof(ProgressStateText))]
	int progressValue;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(ProgressSummary))]
	[NotifyPropertyChangedFor(nameof(ProgressPercentage))]
	[NotifyPropertyChangedFor(nameof(ProgressStateText))]
	int progressMaximum;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsConversionHistoryEmpty))]
	bool hasConversionHistory;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasValidationMessage))]
	string? validationMessage;

	/// <summary>Gets a value indicating whether conversion settings can be changed.</summary>
	public bool CanEditInputs => !IsProcessing;

	/// <summary>Gets the localized primary-action label for the current state.</summary>
	public string StartButtonText =>
		IsProcessing ? Strings.ConvertingImages_Button : Strings.ConvertImages_Button;

	/// <summary>Gets the localized processed-file summary.</summary>
	public string ProgressSummary
	{
		get
		{
			string format = ProgressMaximum == 1
				? Strings.Progress_ProcessedFile
				: Strings.Progress_ProcessedFiles;

			return string.Format(
				CultureInfo.CurrentCulture,
				format,
				ProgressValue,
				ProgressMaximum);
		}
	}

	/// <summary>Gets the rounded completion percentage.</summary>
	public int ProgressPercentage => ProgressMaximum <= 0
		? 0
		: (int)Math.Round(
			(double)ProgressValue / ProgressMaximum * 100,
			MidpointRounding.AwayFromZero);

	/// <summary>Gets the formatted completion percentage.</summary>
	public string ProgressStateText =>
		$"{ProgressPercentage.ToString(CultureInfo.CurrentCulture)}%";

	/// <summary>Gets a value indicating whether a form validation message is visible.</summary>
	public bool HasValidationMessage => !string.IsNullOrEmpty(ValidationMessage);

	/// <summary>Gets a value indicating whether the conversion history is empty.</summary>
	public bool IsConversionHistoryEmpty => !HasConversionHistory;

	/// <summary>Gets or sets the delegate used to pick the source image folder.</summary>
	public Func<Task<string?>>? PickImageFolderDelegate { get; set; }

	/// <summary>Gets or sets the delegate used to pick the target folder.</summary>
	public Func<Task<string?>>? PickOutputFolderDelegate { get; set; }

	[RelayCommand(AllowConcurrentExecutions = false, CanExecute = nameof(CanSelectFolder))]
	async Task SelectImageFolder()
	{
		if (PickImageFolderDelegate is null)
		{
			throw new InvalidOperationException(
				"No folder-pick delegate assigned. The view must assign PickImageFolderDelegate.");
		}

		ImageFolderPath = await PickImageFolderDelegate() ?? string.Empty;
		ValidationMessage = string.IsNullOrEmpty(ImageFolderPath)
			? Strings.Validation_SourceFolderRequired
			: null;
	}

	[RelayCommand(AllowConcurrentExecutions = false, CanExecute = nameof(CanSelectFolder))]
	async Task SelectOutputFolder()
	{
		if (PickOutputFolderDelegate is null)
		{
			throw new InvalidOperationException(
				"No folder-pick delegate assigned. The view must assign PickOutputFolderDelegate.");
		}

		OutputFolderPath = await PickOutputFolderDelegate() ?? string.Empty;
		ValidationMessage = string.IsNullOrEmpty(OutputFolderPath)
			? Strings.Validation_TargetFolderRequired
			: null;
	}

	[RelayCommand(AllowConcurrentExecutions = false, CanExecute = nameof(CanProcessImages))]
	async Task ConvertImagesAsync()
	{
		IsProcessing = true;
		ProgressValue = 0;
		ProgressMaximum = 0;
		ConversionHistory.Clear();
		HasConversionHistory = false;
		ValidationMessage = null;

		OrderedProgress<ImageConversionProgressUpdate> progress = new(ApplyProgressUpdate);

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

			await progress.CompleteAndFlushAsync();

			ProgressMaximum = result.Processed + result.Skipped + result.Failed;
			ProgressValue = ProgressMaximum;
			AddHistoryEntry(ConversionHistoryEntry.CreateSummary(result));
		}
		catch (Exception ex) when (
			ex is ArgumentException
				or IOException
				or UnauthorizedAccessException
				or InvalidOperationException
				or NotSupportedException)
		{
			await progress.CompleteAndFlushAsync();
			Trace.TraceError("Image conversion failed: {0}", ex);
			AddHistoryEntry(ConversionHistoryEntry.CreateFailedSummary(GetBatchErrorMessage(ex)));
		}
		finally
		{
			IsProcessing = false;
		}
	}

	void ApplyProgressUpdate(ImageConversionProgressUpdate update)
	{
		ProgressMaximum = Math.Max(0, update.TotalCount);
		ProgressValue = Math.Clamp(update.ProcessedCount, 0, ProgressMaximum);
		if (update.Status.HasValue)
		{
			AddHistoryEntry(ConversionHistoryEntry.FromProgress(update));
		}
	}

	void AddHistoryEntry(ConversionHistoryEntry entry)
	{
		ConversionHistory.Add(entry);
		HasConversionHistory = true;
	}

	static string GetBatchErrorMessage(Exception exception)
	{
		return exception switch
		{
			ArgumentException => Strings.ConversionError_InvalidPath,
			UnauthorizedAccessException => Strings.ConversionError_AccessDenied,
			IOException => Strings.ConversionError_Io,
			_ => Strings.ConversionError_Generic
		};
	}

	bool CanSelectFolder() => !IsProcessing;

	bool CanProcessImages()
	{
		return !IsProcessing
			&& !string.IsNullOrEmpty(ImageFolderPath)
			&& !string.IsNullOrEmpty(OutputFolderPath);
	}

	sealed class OrderedProgress<T>(Action<T> handler) : IProgress<T>
	{
		readonly Lock synchronizationLock = new();
		readonly SynchronizationContext? synchronizationContext = SynchronizationContext.Current;
		bool isCompleted;

		public void Report(T value)
		{
			lock (synchronizationLock)
			{
				if (isCompleted)
				{
					return;
				}

				if (synchronizationContext is null)
				{
					handler(value);
				}
				else
				{
					synchronizationContext.Post(static state =>
					{
						(T Item, Action<T> Handler) progressState = ((T, Action<T>))state!;
						progressState.Handler(progressState.Item);
					}, (value, handler));
				}
			}
		}

		public Task CompleteAndFlushAsync()
		{
			lock (synchronizationLock)
			{
				if (isCompleted)
				{
					return Task.CompletedTask;
				}

				isCompleted = true;
				if (synchronizationContext is null)
				{
					return Task.CompletedTask;
				}

				TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
				synchronizationContext.Post(
					static state => ((TaskCompletionSource)state!).TrySetResult(),
					completion);
				return completion.Task;
			}
		}
	}
}
