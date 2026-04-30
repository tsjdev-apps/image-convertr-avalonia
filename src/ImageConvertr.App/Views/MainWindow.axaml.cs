using Avalonia.Controls;
using Avalonia.Platform.Storage;
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
		viewModel.PickImageFolderDelegate = () => PickFolderAsync("Please select the folder containing your images");
		viewModel.PickOutputFolderDelegate = () => PickFolderAsync("Please select the output folder");
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
}
