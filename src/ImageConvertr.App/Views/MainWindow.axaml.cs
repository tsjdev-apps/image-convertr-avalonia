using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using ImageConvertr.App.Resources.Localization;
using ImageConvertr.App.ViewModels;

namespace ImageConvertr.App.Views;

/// <summary>
/// Hosts the main image conversion user interface.
/// </summary>
public partial class MainWindow : Window
{
	/// <summary>
	/// Initializes a new instance of the <see cref="MainWindow"/> class.
	/// </summary>
	public MainWindow()
	{
		InitializeComponent();
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MainWindow"/> class with its view model.
	/// </summary>
	/// <param name="viewModel">The view model used as the data context.</param>
	public MainWindow(MainWindowViewModel viewModel)
		: this()
	{
		ArgumentNullException.ThrowIfNull(viewModel);

		DataContext = viewModel;
		viewModel.PickImageFolderDelegate = () => PickFolderAsync(Strings.SourceFolder_PickerTitle);
		viewModel.PickOutputFolderDelegate = () => PickFolderAsync(Strings.TargetFolder_PickerTitle);
		viewModel.ConversionHistory.CollectionChanged += ConversionHistoryOnCollectionChanged;
	}

	/// <summary>
	/// Opens a folder picker and returns the selected local path.
	/// </summary>
	async Task<string?> PickFolderAsync(string title)
	{
		IReadOnlyList<IStorageFolder> folders = await StorageProvider.OpenFolderPickerAsync(
			new FolderPickerOpenOptions
			{
				AllowMultiple = false,
				Title = title
			});

		return folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
	}

	void ConversionHistoryOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		if (e.Action != NotifyCollectionChangedAction.Add
			|| DataContext is not MainWindowViewModel { IsProcessing: true } viewModel
			|| viewModel.ConversionHistory.Count == 0)
		{
			return;
		}

		ConversionHistoryEntry newestEntry = viewModel.ConversionHistory[^1];
		Dispatcher.UIThread.Post(
			() => ConversionHistoryListBox.ScrollIntoView(newestEntry),
			DispatcherPriority.Background);
	}
}
