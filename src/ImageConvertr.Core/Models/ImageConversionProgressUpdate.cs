namespace ImageConvertr.Core.Models;

/// <summary>
/// Represents a progress update for a batch image conversion operation.
/// </summary>
/// <param name="ProcessedCount">The number of files whose processing has finished.</param>
/// <param name="TotalCount">The total number of files discovered in the batch.</param>
/// <param name="SourceFileName">The source file name without its directory, or <see langword="null"/> for batch initialization.</param>
/// <param name="TargetFileName">The intended target file name without its directory, or <see langword="null"/> for batch initialization.</param>
/// <param name="SourceFormat">The source file extension without a leading dot, or <see langword="null"/> for batch initialization.</param>
/// <param name="TargetFormat">The selected output format name.</param>
/// <param name="Status">The final outcome for the source file, or <see langword="null"/> for batch initialization.</param>
/// <param name="SourceFileSize">The source file size when it was readily available.</param>
/// <param name="TargetFileSize">The target file size after a successful conversion.</param>
/// <param name="ErrorKind">The structured reason for a skipped or failed conversion.</param>
public sealed record ImageConversionProgressUpdate(
	int ProcessedCount,
	int TotalCount,
	string? SourceFileName,
	string? TargetFileName,
	string? SourceFormat,
	string TargetFormat,
	ConversionStatus? Status,
	long? SourceFileSize = null,
	long? TargetFileSize = null,
	ConversionErrorKind ErrorKind = ConversionErrorKind.None);
