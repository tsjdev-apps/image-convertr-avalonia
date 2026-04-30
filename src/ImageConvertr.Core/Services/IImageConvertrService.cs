using ImageConvertr.Core.Models;

namespace ImageConvertr.Core.Services;

/// <summary>
/// Provides functionality to convert supported image files in a specified folder using SkiaSharp.
/// </summary>
/// <remarks>This interface is intended for batch conversion scenarios where multiple images need to be processed
/// efficiently. Implementations should support common image formats such as JPEG, PNG, and BMP. Thread safety
/// and error handling are implementation-specific and should be considered when using this service.</remarks>
public interface IImageConvertrService
{
	/// <summary>
	/// Reads all supported image files from the input folder, converts them to the selected format using SkiaSharp,
	/// and saves them into the output folder.
	/// </summary>
	/// <param name="imageInputPath">Source folder containing images.</param>
	/// <param name="imageOutputPath">Destination folder for generated files.</param>
	/// <param name="outputFormat">Selected output image format.</param>
	/// <param name="imageQuality">Image quality (0-100) for lossy formats. Recommended range: 70-90.</param>
	/// <param name="overwriteExisting">If true, existing output files will be overwritten.</param>
	/// <param name="progress">Optional progress sink for live conversion updates.</param>
	/// <returns>
	/// A result object containing:
	/// - Number of processed images
	/// - Number of skipped images
	/// - Number of failed images
	/// - Total saved megabytes
	/// </returns>
	ImageConvertResult ConvertFolder(
		string imageInputPath,
		string imageOutputPath,
		ImageOutputFormatOption outputFormat,
		int imageQuality = 85,
		bool overwriteExisting = false,
		IProgress<ImageConversionProgressUpdate>? progress = null);
}
