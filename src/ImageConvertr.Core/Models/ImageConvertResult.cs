namespace ImageConvertr.Core.Models;

/// <summary>
/// Represents the result of a batch image conversion operation.
/// </summary>
/// <param name="Processed">
/// The number of images successfully converted and written to the output directory.
/// </param>
/// <param name="Skipped">
/// The number of images that were skipped.
/// This can happen if the file format is unsupported or if the output file already exists
/// and overwriting was disabled.
/// </param>
/// <param name="Failed">
/// The number of images that could not be processed due to errors
/// (e.g., decoding or encoding failures).
/// </param>
/// <param name="SavedMB">
/// The total number of megabytes saved through conversion, calculated as the sum of
/// (original file size - output file size) for all successfully processed images.
/// Only positive savings are included.
/// </param>
public sealed record ImageConvertResult(
	int Processed,
	int Skipped,
	int Failed,
	double SavedMB
);
