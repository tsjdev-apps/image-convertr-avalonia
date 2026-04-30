using System.Globalization;
using ImageConvertr.Core.Models;
using SkiaSharp;

namespace ImageConvertr.Core.Services;

/// <summary>
/// Provides services for converting image files in a folder to the selected format, supporting configurable quality and
/// overwrite options.
/// </summary>
/// <remarks>Supported input formats include JPEG, PNG, WebP, BMP, and GIF. The service processes only files with
/// allowed extensions and skips unsupported or existing files unless overwriting is enabled. Progress is reported via
/// the optional progress sink.</remarks>
public sealed class ImageConvertrService : IImageConvertrService
{
	/// <summary>
	/// The file extensions accepted as conversion input.
	/// </summary>
	readonly HashSet<string> allowedExtensions = new(StringComparer.OrdinalIgnoreCase)
	{
		".jpg", ".jpeg", ".png", ".webp", ".bmp", ".gif"
	};

	/// <inheritdoc/>
	public ImageConvertResult ConvertFolder(
		string imageInputPath,
		string imageOutputPath,
		ImageOutputFormatOption outputFormat,
		int imageQuality = 85,
		bool overwriteExisting = false,
		IProgress<ImageConversionProgressUpdate>? progress = null)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(imageInputPath);
		ArgumentException.ThrowIfNullOrWhiteSpace(imageOutputPath);
		ArgumentNullException.ThrowIfNull(outputFormat);

		imageQuality = Math.Clamp(imageQuality, 0, 100);

		Directory.CreateDirectory(imageOutputPath);

		int processed = 0;
		int skipped = 0;
		int failed = 0;
		long totalSavedBytes = 0;
		string[] files = Directory.EnumerateFiles(imageInputPath, "*", SearchOption.TopDirectoryOnly).ToArray();
		string[] imageFiles = files
			.Where(file => IsSupportedImage(file))
			.ToArray();

		skipped = files.Length - imageFiles.Length;
		int totalImages = imageFiles.Length;

		for (int index = 0; index < imageFiles.Length; index++)
		{
			string file = imageFiles[index];
			int currentIndex = index + 1;
			FileInfo inputInfo = new(file);

			progress?.Report(new ImageConversionProgressUpdate(
				currentIndex,
				totalImages,
				$"Converting '{inputInfo.Name}' to {outputFormat.DisplayName}."));

			string outputFileName =
				Path.GetFileNameWithoutExtension(file) + outputFormat.FileExtension;
			string outputPath = Path.Combine(imageOutputPath, outputFileName);

			void ReportFailedConversion()
			{
				failed++;
				progress?.Report(new ImageConversionProgressUpdate(
					currentIndex,
					totalImages,
					$"Failed to convert '{Path.GetFileName(file)}'.",
					LogLevel.Error));
			}

			if (Path.GetFullPath(file).Equals(
				Path.GetFullPath(outputPath),
				StringComparison.OrdinalIgnoreCase))
			{
				skipped++;
				progress?.Report(new ImageConversionProgressUpdate(
					currentIndex,
					totalImages,
					$"Skipped '{inputInfo.Name}' because the source and output paths are identical.",
					LogLevel.Info));
				continue;
			}

			if (!overwriteExisting && File.Exists(outputPath))
			{
				skipped++;
				progress?.Report(new ImageConversionProgressUpdate(
					currentIndex,
					totalImages,
					$"Skipped '{inputInfo.Name}' because '{outputFileName}' already exists in the output folder.",
					LogLevel.Info));
				continue;
			}

			try
			{
				using FileStream inputStream = File.OpenRead(file);
				using SKBitmap? bitmap = SKBitmap.Decode(inputStream);

				if (bitmap is null)
				{
					failed++;
					progress?.Report(new ImageConversionProgressUpdate(
						currentIndex,
						totalImages,
						$"Failed to convert '{inputInfo.Name}' because it could not be decoded.",
						LogLevel.Error));
					continue;
				}

				using SKImage image = SKImage.FromBitmap(bitmap);
				using SKData? encodedData = image.Encode(outputFormat.EncodedImageFormat, imageQuality);

				if (encodedData is null)
				{
					failed++;
					progress?.Report(new ImageConversionProgressUpdate(
						currentIndex,
						totalImages,
						$"Failed to convert '{inputInfo.Name}' because it could not be encoded.",
						LogLevel.Error));
					continue;
				}

				FileMode fileMode =
					overwriteExisting ? FileMode.Create : FileMode.CreateNew;

				using (FileStream outputStream = new(outputPath, fileMode, FileAccess.Write, FileShare.None))
				{
					encodedData.SaveTo(outputStream);
				}

				processed++;

				long outputBytes = new FileInfo(outputPath).Length;
				long savedBytes = inputInfo.Length - outputBytes;

				if (savedBytes > 0)
				{
					totalSavedBytes += savedBytes;
				}

				progress?.Report(new ImageConversionProgressUpdate(
					currentIndex,
					totalImages,
					$"Converted '{inputInfo.Name}' to {outputFormat.DisplayName}.",
					LogLevel.Success));
			}
			catch (IOException)
			{
				ReportFailedConversion();
			}
			catch (UnauthorizedAccessException)
			{
				ReportFailedConversion();
			}
			catch (ArgumentException)
			{
				ReportFailedConversion();
			}
			catch (InvalidOperationException)
			{
				ReportFailedConversion();
			}
			catch (NotSupportedException)
			{
				ReportFailedConversion();
			}
		}

		double savedMB = totalSavedBytes / (1024.0 * 1024.0);

		return new ImageConvertResult(
			processed,
			skipped,
			failed,
			Math.Round(savedMB, 2));
	}

	bool IsSupportedImage(string file)
	{
		string extension = Path.GetExtension(file);

		return !string.IsNullOrEmpty(extension)
			&& allowedExtensions.Contains(extension);
	}
}
