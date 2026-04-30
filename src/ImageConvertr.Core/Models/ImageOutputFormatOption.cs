using SkiaSharp;

namespace ImageConvertr.Core.Models;

/// <summary>
/// Describes an image format that can be selected as conversion output.
/// </summary>
public sealed record ImageOutputFormatOption(
	string DisplayName,
	string FileExtension,
	SKEncodedImageFormat EncodedImageFormat)
{
	/// <summary>
	/// Gets the PNG output format option.
	/// </summary>
	public static ImageOutputFormatOption Png { get; } =
		new("PNG", ".png", SKEncodedImageFormat.Png);

	/// <summary>
	/// Gets the JPEG output format option.
	/// </summary>
	public static ImageOutputFormatOption Jpeg { get; } =
		new("JPEG", ".jpg", SKEncodedImageFormat.Jpeg);

	/// <summary>
	/// Gets the WebP output format option.
	/// </summary>
	public static ImageOutputFormatOption Webp { get; } =
		new("WebP", ".webp", SKEncodedImageFormat.Webp);

	/// <summary>
	/// Gets all supported output format options in display order.
	/// </summary>
	public static IReadOnlyList<ImageOutputFormatOption> All { get; } =
		[Png, Jpeg, Webp];
}
