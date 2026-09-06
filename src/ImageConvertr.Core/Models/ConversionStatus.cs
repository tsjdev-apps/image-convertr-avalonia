namespace ImageConvertr.Core.Models;

/// <summary>
/// Describes the final outcome of a single image conversion attempt.
/// </summary>
public enum ConversionStatus
{
	/// <summary>The image was converted to a new file.</summary>
	Converted,

	/// <summary>The image was not converted.</summary>
	Skipped,

	/// <summary>An existing target file was replaced successfully.</summary>
	Overwritten,

	/// <summary>The image could not be converted.</summary>
	Failed
}
