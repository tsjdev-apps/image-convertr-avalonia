using ImageConvertr.Core.Models;
using SkiaSharp;

namespace ImageConvertr.Tests;

/// <summary>
/// Verifies the output format option metadata.
/// </summary>
public sealed class ImageOutputFormatOptionTests
{
	/// <summary>
	/// Verifies that all supported output formats are exposed in display order.
	/// </summary>
	[Fact]
	public void AllExposesSupportedOutputFormatsInDisplayOrder()
	{
		ImageOutputFormatOption[] formats = ImageOutputFormatOption.All.ToArray();

		Assert.Equal(
			[
				ImageOutputFormatOption.Png,
				ImageOutputFormatOption.Jpeg,
				ImageOutputFormatOption.Webp
			],
			formats);
	}

	/// <summary>
	/// Verifies that each static format exposes the expected metadata.
	/// </summary>
	[Theory]
	[InlineData("PNG", ".png", SKEncodedImageFormat.Png)]
	[InlineData("JPEG", ".jpg", SKEncodedImageFormat.Jpeg)]
	[InlineData("WebP", ".webp", SKEncodedImageFormat.Webp)]
	public void StaticFormatsExposeExpectedMetadata(
		string displayName,
		string fileExtension,
		SKEncodedImageFormat encodedImageFormat)
	{
		ImageOutputFormatOption format = Assert.Single(
			ImageOutputFormatOption.All,
			option => option.DisplayName == displayName);

		Assert.Equal(fileExtension, format.FileExtension);
		Assert.Equal(encodedImageFormat, format.EncodedImageFormat);
	}
}
