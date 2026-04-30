namespace ImageConvertr.Core.Models;

/// <summary>
/// Represents a progress update for a batch image conversion operation.
/// </summary>
/// <param name="CurrentIndex">The one-based index of the image currently being handled.</param>
/// <param name="TotalCount">The total number of supported images in the batch.</param>
/// <param name="Message">The user-facing progress message.</param>
/// <param name="Level">The severity level for the progress message.</param>
public sealed record ImageConversionProgressUpdate(
	int CurrentIndex,
	int TotalCount,
	string Message,
	LogLevel Level = LogLevel.Info);
