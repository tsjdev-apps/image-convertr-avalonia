using System.Text;
using ImageConvertr.Core.Models;
using ImageConvertr.Core.Services;
using SkiaSharp;

namespace ImageConvertr.Tests;

/// <summary>
/// Verifies the image conversion service behavior.
/// </summary>
public sealed class ImageConvertrServiceTests
{
	const int testImageWidth = 12;
	const int testImageHeight = 8;
	const string singlePixelGifBase64 = "R0lGODlhAQABAIAAAAAAAP///ywAAAAAAQABAAACAUwAOw==";

	/// <summary>
	/// Verifies that supported files are converted while unsupported files are counted as skipped.
	/// </summary>
	[Fact]
	public void ConvertFolderConvertsSupportedImagesAndSkipsUnsupportedFiles()
	{
		using TestWorkspace workspace = new();
		workspace.CreateInputImage("camera.png");
		workspace.CreateInputFile("notes.txt", "not an image");
		ListProgress progress = new();
		ImageConvertrService service = new();

		ImageConvertResult result = service.ConvertFolder(
			workspace.InputFolder,
			workspace.OutputFolder,
			ImageOutputFormatOption.Jpeg,
			imageQuality: 90,
			overwriteExisting: false,
			progress);

		string outputPath = Path.Combine(workspace.OutputFolder, "camera.jpg");

		Assert.Equal(1, result.Processed);
		Assert.Equal(1, result.Skipped);
		Assert.Equal(0, result.Failed);
		AssertDecodableImage(outputPath);
		Assert.Collection(
			progress.Updates,
			update =>
			{
				Assert.Equal(0, update.ProcessedCount);
				Assert.Equal(2, update.TotalCount);
				Assert.Null(update.Status);
			},
			update =>
			{
				Assert.Equal(1, update.ProcessedCount);
				Assert.Equal(2, update.TotalCount);
				Assert.Equal("camera.png", update.SourceFileName);
				Assert.Equal("camera.jpg", update.TargetFileName);
				Assert.Equal(ConversionStatus.Converted, update.Status);
				Assert.Equal(ConversionErrorKind.None, update.ErrorKind);
			},
			update =>
			{
				Assert.Equal(2, update.ProcessedCount);
				Assert.Equal(ConversionStatus.Skipped, update.Status);
				Assert.Equal(ConversionErrorKind.UnsupportedFormat, update.ErrorKind);
			});
	}

	/// <summary>
	/// Verifies that real GIF input files are decoded and converted.
	/// </summary>
	[Fact]
	public void ConvertFolderConvertsGifInputImages()
	{
		using TestWorkspace workspace = new();
		const string fileName = "camera.gif";
		workspace.CreateInputImage(fileName);
		ListProgress progress = new();
		ImageConvertrService service = new();

		ImageConvertResult result = service.ConvertFolder(
			workspace.InputFolder,
			workspace.OutputFolder,
			ImageOutputFormatOption.Png,
			imageQuality: 90,
			overwriteExisting: false,
			progress);

		string outputPath = Path.Combine(
			workspace.OutputFolder,
			Path.GetFileNameWithoutExtension(fileName) + ".png");

		Assert.Equal(1, result.Processed);
		Assert.Equal(0, result.Skipped);
		Assert.Equal(0, result.Failed);
		AssertDecodableImage(outputPath, expectedWidth: 1, expectedHeight: 1);
		Assert.Contains(progress.Updates, update =>
			update.SourceFileName == fileName
			&& update.TargetFileName == "camera.png"
			&& update.Status == ConversionStatus.Converted);
	}

	/// <summary>
	/// Verifies that TIFF input files are skipped because TIFF is not supported by the current pipeline.
	/// </summary>
	[Theory]
	[InlineData("camera.tif")]
	[InlineData("camera.tiff")]
	public void ConvertFolderSkipsTiffInputImagesBecauseTiffIsNotSupported(string fileName)
	{
		using TestWorkspace workspace = new();
		workspace.CreateInputImage(fileName);
		ListProgress progress = new();
		ImageConvertrService service = new();

		ImageConvertResult result = service.ConvertFolder(
			workspace.InputFolder,
			workspace.OutputFolder,
			ImageOutputFormatOption.Png,
			imageQuality: 90,
			overwriteExisting: false,
			progress);

		Assert.Equal(0, result.Processed);
		Assert.Equal(1, result.Skipped);
		Assert.Equal(0, result.Failed);
		Assert.Empty(Directory.EnumerateFiles(workspace.OutputFolder));
		Assert.Equal(2, progress.Updates.Count);
		ImageConversionProgressUpdate update = progress.Updates[1];
		Assert.Equal(ConversionStatus.Skipped, update.Status);
		Assert.Equal(ConversionErrorKind.UnsupportedFormat, update.ErrorKind);
	}

	/// <summary>
	/// Verifies that existing outputs are skipped when overwrite is disabled.
	/// </summary>
	[Fact]
	public void ConvertFolderSkipsExistingOutputWhenOverwriteIsDisabled()
	{
		using TestWorkspace workspace = new();
		workspace.CreateInputImage("camera.png");
		string existingOutputPath = workspace.CreateOutputFile("camera.jpg", "existing content");
		ListProgress progress = new();
		ImageConvertrService service = new();

		ImageConvertResult result = service.ConvertFolder(
			workspace.InputFolder,
			workspace.OutputFolder,
			ImageOutputFormatOption.Jpeg,
			imageQuality: 80,
			overwriteExisting: false,
			progress);

		Assert.Equal(0, result.Processed);
		Assert.Equal(1, result.Skipped);
		Assert.Equal(0, result.Failed);
		Assert.Equal("existing content", File.ReadAllText(existingOutputPath));
		Assert.Contains(progress.Updates, update =>
			update.SourceFileName == "camera.png"
			&& update.TargetFileName == "camera.jpg"
			&& update.Status == ConversionStatus.Skipped
			&& update.ErrorKind == ConversionErrorKind.TargetExists);
	}

	/// <summary>
	/// Verifies that existing outputs are replaced when overwrite is enabled.
	/// </summary>
	[Fact]
	public void ConvertFolderOverwritesExistingOutputWhenOverwriteIsEnabled()
	{
		using TestWorkspace workspace = new();
		workspace.CreateInputImage("camera.png");
		byte[] existingBytes = Encoding.UTF8.GetBytes("existing content");
		string existingOutputPath = workspace.CreateOutputFile("camera.jpg", existingBytes);
		ImageConvertrService service = new();

		ImageConvertResult result = service.ConvertFolder(
			workspace.InputFolder,
			workspace.OutputFolder,
			ImageOutputFormatOption.Jpeg,
			imageQuality: 80,
			overwriteExisting: true);

		Assert.Equal(1, result.Processed);
		Assert.Equal(0, result.Skipped);
		Assert.Equal(0, result.Failed);
		Assert.Equal(1, result.Overwritten);
		Assert.NotEqual(existingBytes, File.ReadAllBytes(existingOutputPath));
		AssertDecodableImage(existingOutputPath);
	}

	/// <summary>
	/// Verifies that the output directory is created when it does not already exist.
	/// </summary>
	[Fact]
	public void ConvertFolderCreatesOutputDirectoryWhenItDoesNotExist()
	{
		using TestWorkspace workspace = new();
		workspace.CreateInputImage("camera.png");
		string missingOutputFolder = Path.Combine(workspace.RootFolder, "missing-output");
		ImageConvertrService service = new();

		ImageConvertResult result = service.ConvertFolder(
			workspace.InputFolder,
			missingOutputFolder,
			ImageOutputFormatOption.Png,
			imageQuality: 250,
			overwriteExisting: false);

		Assert.Equal(1, result.Processed);
		Assert.True(Directory.Exists(missingOutputFolder));
		AssertDecodableImage(Path.Combine(missingOutputFolder, "camera.png"));
	}

	/// <summary>
	/// Verifies that a file is skipped when its source and output paths are identical.
	/// </summary>
	[Fact]
	public void ConvertFolderSkipsImageWhenSourceAndOutputPathsAreIdentical()
	{
		using TestWorkspace workspace = new();
		string inputPath = workspace.CreateInputImage("camera.png");
		ListProgress progress = new();
		ImageConvertrService service = new();

		ImageConvertResult result = service.ConvertFolder(
			workspace.InputFolder,
			workspace.InputFolder,
			ImageOutputFormatOption.Png,
			imageQuality: 80,
			overwriteExisting: true,
			progress);

		Assert.Equal(0, result.Processed);
		Assert.Equal(1, result.Skipped);
		Assert.Equal(0, result.Failed);
		Assert.True(File.Exists(inputPath));
		Assert.Contains(progress.Updates, update =>
			update.Status == ConversionStatus.Skipped
			&& update.ErrorKind == ConversionErrorKind.SourceMatchesTarget);
	}

	/// <summary>
	/// Verifies that a supported file with invalid image data is counted as failed.
	/// </summary>
	[Fact]
	public void ConvertFolderCountsFailedImageWhenSupportedFileCannotBeDecoded()
	{
		using TestWorkspace workspace = new();
		workspace.CreateInputFile("broken.jpg", "not image data");
		ListProgress progress = new();
		ImageConvertrService service = new();

		ImageConvertResult result = service.ConvertFolder(
			workspace.InputFolder,
			workspace.OutputFolder,
			ImageOutputFormatOption.Png,
			imageQuality: 80,
			overwriteExisting: false,
			progress);

		Assert.Equal(0, result.Processed);
		Assert.Equal(0, result.Skipped);
		Assert.Equal(1, result.Failed);
		Assert.Empty(Directory.EnumerateFiles(workspace.OutputFolder));
		Assert.Contains(progress.Updates, update =>
			update.SourceFileName == "broken.jpg"
			&& update.Status == ConversionStatus.Failed
			&& update.ErrorKind == ConversionErrorKind.DecodeFailed);
	}

	/// <summary>
	/// Verifies that image quality is clamped before encoding.
	/// </summary>
	[Fact]
	public void ConvertFolderClampsQualityBeforeEncoding()
	{
		using TestWorkspace workspace = new();
		workspace.CreateInputImage("camera.png");
		ImageConvertrService service = new();

		ImageConvertResult result = service.ConvertFolder(
			workspace.InputFolder,
			workspace.OutputFolder,
			ImageOutputFormatOption.Jpeg,
			imageQuality: -25,
			overwriteExisting: false);

		string outputPath = Path.Combine(workspace.OutputFolder, "camera.jpg");

		Assert.Equal(1, result.Processed);
		Assert.Equal(0, result.Failed);
		AssertDecodableImage(outputPath);
	}

	static void AssertDecodableImage(
		string path,
		int expectedWidth = testImageWidth,
		int expectedHeight = testImageHeight)
	{
		using FileStream stream = File.OpenRead(path);
		using SKCodec? codec = SKCodec.Create(stream);

		Assert.NotNull(codec);
		Assert.Equal(expectedWidth, codec!.Info.Width);
		Assert.Equal(expectedHeight, codec.Info.Height);
	}

	sealed class ListProgress : IProgress<ImageConversionProgressUpdate>
	{
		public List<ImageConversionProgressUpdate> Updates { get; } = [];

		public void Report(ImageConversionProgressUpdate value)
		{
			Updates.Add(value);
		}
	}

	sealed class TestWorkspace : IDisposable
	{
		public TestWorkspace()
		{
			RootFolder = Path.Combine(Path.GetTempPath(), "ImageConvertr.Tests", Guid.NewGuid().ToString("N"));
			InputFolder = Path.Combine(RootFolder, "input");
			OutputFolder = Path.Combine(RootFolder, "output");

			Directory.CreateDirectory(InputFolder);
			Directory.CreateDirectory(OutputFolder);
		}

		public string RootFolder { get; }

		public string InputFolder { get; }

		public string OutputFolder { get; }

		public void Dispose()
		{
			if (Directory.Exists(RootFolder))
			{
				Directory.Delete(RootFolder, recursive: true);
			}
		}

		public string CreateInputFile(string fileName, string content)
		{
			string path = Path.Combine(InputFolder, fileName);
			File.WriteAllText(path, content);
			return path;
		}

		public string CreateOutputFile(string fileName, string content)
		{
			return CreateOutputFile(fileName, Encoding.UTF8.GetBytes(content));
		}

		public string CreateOutputFile(string fileName, byte[] content)
		{
			string path = Path.Combine(OutputFolder, fileName);
			File.WriteAllBytes(path, content);
			return path;
		}

		public string CreateInputImage(string fileName)
		{
			string path = Path.Combine(InputFolder, fileName);
			string extension = Path.GetExtension(fileName);

			using SKBitmap bitmap = new(testImageWidth, testImageHeight);
			bitmap.Erase(new SKColor(20, 132, 245));

			byte[] imageBytes = extension.ToUpperInvariant() switch
			{
				".GIF" => Convert.FromBase64String(singlePixelGifBase64),
				".TIF" or ".TIFF" => CreateTiffImageBytes(),
				_ => EncodeBitmap(bitmap, SKEncodedImageFormat.Png)
			};

			File.WriteAllBytes(path, imageBytes);
			return path;
		}

		static byte[] EncodeBitmap(SKBitmap bitmap, SKEncodedImageFormat encodedImageFormat)
		{
			using SKImage image = SKImage.FromBitmap(bitmap);
			using SKData? data = image.Encode(encodedImageFormat, quality: 100);

			if (data is null)
			{
				throw new InvalidOperationException("The test image could not be encoded.");
			}

			return data.ToArray();
		}

		static byte[] CreateTiffImageBytes()
		{
			const ushort imageFileDirectoryEntryCount = 10;
			const ushort tiffShort = 3;
			const ushort tiffLong = 4;
			const int samplesPerPixel = 3;
			const int bitsPerSampleCount = 3;
			const int imageFileDirectoryOffset = 8;

			int bitsPerSampleOffset = imageFileDirectoryOffset
				+ sizeof(ushort)
				+ imageFileDirectoryEntryCount * 12
				+ sizeof(uint);
			int pixelDataOffset = bitsPerSampleOffset + bitsPerSampleCount * sizeof(ushort);
			int stripByteCount = testImageWidth * testImageHeight * samplesPerPixel;

			using MemoryStream stream = new();
			using BinaryWriter writer = new(stream);

			writer.Write((byte)'I');
			writer.Write((byte)'I');
			writer.Write((ushort)42);
			writer.Write((uint)imageFileDirectoryOffset);
			writer.Write(imageFileDirectoryEntryCount);

			WriteTiffEntry(writer, tag: 256, tiffLong, count: 1, valueOrOffset: testImageWidth);
			WriteTiffEntry(writer, tag: 257, tiffLong, count: 1, valueOrOffset: testImageHeight);
			WriteTiffEntry(writer, tag: 258, tiffShort, count: bitsPerSampleCount, valueOrOffset: bitsPerSampleOffset);
			WriteTiffEntry(writer, tag: 259, tiffShort, count: 1, valueOrOffset: 1);
			WriteTiffEntry(writer, tag: 262, tiffShort, count: 1, valueOrOffset: 2);
			WriteTiffEntry(writer, tag: 273, tiffLong, count: 1, valueOrOffset: pixelDataOffset);
			WriteTiffEntry(writer, tag: 277, tiffShort, count: 1, valueOrOffset: samplesPerPixel);
			WriteTiffEntry(writer, tag: 278, tiffLong, count: 1, valueOrOffset: testImageHeight);
			WriteTiffEntry(writer, tag: 279, tiffLong, count: 1, valueOrOffset: stripByteCount);
			WriteTiffEntry(writer, tag: 284, tiffShort, count: 1, valueOrOffset: 1);
			writer.Write((uint)0);

			writer.Write((ushort)8);
			writer.Write((ushort)8);
			writer.Write((ushort)8);

			for (int y = 0; y < testImageHeight; y++)
			{
				for (int x = 0; x < testImageWidth; x++)
				{
					writer.Write((byte)(20 + x * 3));
					writer.Write((byte)(132 + y * 2));
					writer.Write((byte)(245 - y * 4));
				}
			}

			return stream.ToArray();
		}

		static void WriteTiffEntry(
			BinaryWriter writer,
			ushort tag,
			ushort type,
			uint count,
			int valueOrOffset)
		{
			writer.Write(tag);
			writer.Write(type);
			writer.Write(count);
			writer.Write((uint)valueOrOffset);
		}
	}
}
