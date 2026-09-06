namespace ImageConvertr.Core.Models;

/// <summary>
/// Identifies a user-presentable reason for a skipped or failed conversion.
/// </summary>
public enum ConversionErrorKind
{
	/// <summary>No error occurred.</summary>
	None,

	/// <summary>The target file already exists.</summary>
	TargetExists,

	/// <summary>The source and target resolve to the same path.</summary>
	SourceMatchesTarget,

	/// <summary>The input file extension is not supported.</summary>
	UnsupportedFormat,

	/// <summary>The image data could not be decoded.</summary>
	DecodeFailed,

	/// <summary>The image could not be encoded in the selected format.</summary>
	EncodeFailed,

	/// <summary>A file-system input/output operation failed.</summary>
	IoError,

	/// <summary>The application did not have permission to access a file.</summary>
	AccessDenied,

	/// <summary>The conversion failed for another known, handled reason.</summary>
	Generic
}
