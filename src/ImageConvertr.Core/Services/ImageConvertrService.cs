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
		int overwritten = 0;
		long totalSavedBytes = 0;
		string[] files = Directory.EnumerateFiles(imageInputPath, "*", SearchOption.TopDirectoryOnly).ToArray();
		int totalFiles = files.Length;
		progress?.Report(new ImageConversionProgressUpdate(
			ProcessedCount: 0,
			TotalCount: totalFiles,
			SourceFileName: null,
			TargetFileName: null,
			SourceFormat: null,
			TargetFormat: outputFormat.DisplayName,
			Status: null));

		for (int index = 0; index < files.Length; index++)
		{
			string file = files[index];
			int processedCount = index + 1;
			FileInfo inputInfo = new(file);
			string outputFileName =
				Path.GetFileNameWithoutExtension(file) + outputFormat.FileExtension;
			string outputPath = Path.Combine(imageOutputPath, outputFileName);
			string sourceFormat = Path.GetExtension(file).TrimStart('.').ToUpperInvariant();

			void Report(
				ConversionStatus status,
				ConversionErrorKind errorKind = ConversionErrorKind.None,
				long? sourceFileSize = null,
				long? targetFileSize = null)
			{
				progress?.Report(new ImageConversionProgressUpdate(
					processedCount,
					totalFiles,
					inputInfo.Name,
					outputFileName,
					sourceFormat,
					outputFormat.DisplayName,
					status,
					sourceFileSize,
					targetFileSize,
					errorKind));
			}

			void ReportFailedConversion(ConversionErrorKind errorKind)
			{
				failed++;
				Report(ConversionStatus.Failed, errorKind);
			}

			if (!IsSupportedImage(file))
			{
				skipped++;
				Report(ConversionStatus.Skipped, ConversionErrorKind.UnsupportedFormat);
				continue;
			}

			if (Path.GetFullPath(file).Equals(
				Path.GetFullPath(outputPath),
				StringComparison.OrdinalIgnoreCase))
			{
				skipped++;
				Report(ConversionStatus.Skipped, ConversionErrorKind.SourceMatchesTarget);
				continue;
			}

			bool targetExisted = File.Exists(outputPath);

			if (!overwriteExisting && targetExisted)
			{
				skipped++;
				Report(ConversionStatus.Skipped, ConversionErrorKind.TargetExists);
				continue;
			}

			try
			{
				using FileStream inputStream = File.OpenRead(file);
				using SKBitmap? bitmap = SKBitmap.Decode(inputStream);

				if (bitmap is null)
				{
					ReportFailedConversion(ConversionErrorKind.DecodeFailed);
					continue;
				}

				using SKImage image = SKImage.FromBitmap(bitmap);
				using SKData? encodedData = image.Encode(outputFormat.EncodedImageFormat, imageQuality);

				if (encodedData is null)
				{
					ReportFailedConversion(ConversionErrorKind.EncodeFailed);
					continue;
				}

				FileMode fileMode =
					overwriteExisting ? FileMode.Create : FileMode.CreateNew;

				using (FileStream outputStream = new(outputPath, fileMode, FileAccess.Write, FileShare.None))
				{
					encodedData.SaveTo(outputStream);
				}

				processed++;
				if (targetExisted)
				{
					overwritten++;
				}

				long inputBytes = inputInfo.Length;
				long outputBytes = new FileInfo(outputPath).Length;
				long savedBytes = inputBytes - outputBytes;

				if (savedBytes > 0)
				{
					totalSavedBytes += savedBytes;
				}

				Report(
					targetExisted ? ConversionStatus.Overwritten : ConversionStatus.Converted,
					sourceFileSize: inputBytes,
					targetFileSize: outputBytes);
			}
			catch (IOException)
			{
				ReportFailedConversion(ConversionErrorKind.IoError);
			}
			catch (UnauthorizedAccessException)
			{
				ReportFailedConversion(ConversionErrorKind.AccessDenied);
			}
			catch (ArgumentException)
			{
				ReportFailedConversion(ConversionErrorKind.Generic);
			}
			catch (InvalidOperationException)
			{
				ReportFailedConversion(ConversionErrorKind.Generic);
			}
			catch (NotSupportedException)
			{
				ReportFailedConversion(ConversionErrorKind.Generic);
			}
		}

		double savedMB = totalSavedBytes / (1024.0 * 1024.0);

		return new ImageConvertResult(
			processed,
			skipped,
			failed,
			Math.Round(savedMB, 2),
			overwritten);
	}

	bool IsSupportedImage(string file)
	{
		string extension = Path.GetExtension(file);

		return !string.IsNullOrEmpty(extension)
			&& allowedExtensions.Contains(extension);
	}
}
