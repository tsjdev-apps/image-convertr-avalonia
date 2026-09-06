using System.Globalization;
using ImageConvertr.App.Resources.Localization;
using ImageConvertr.Core.Models;

namespace ImageConvertr.App.ViewModels;

#pragma warning disable CA1863 // Localized composite formats can change with the active UI culture.

/// <summary>
/// Represents one compact, localized row in the conversion history.
/// </summary>
public sealed class ConversionHistoryEntry
{
	ConversionHistoryEntry(
		string sourceFileName,
		string resultText,
		string detailText,
		string? fileSizeText,
		string statusSymbol,
		ConversionStatus? status,
		bool isSummary)
	{
		SourceFileName = sourceFileName;
		ResultText = resultText;
		DetailText = detailText;
		FileSizeText = fileSizeText;
		StatusSymbol = statusSymbol;
		Status = status;
		IsSummary = isSummary;
	}

	/// <summary>Gets the source file name or summary heading.</summary>
	public string SourceFileName { get; }

	/// <summary>Gets the primary result text.</summary>
	public string ResultText { get; }

	/// <summary>Gets format or summary details.</summary>
	public string DetailText { get; }

	/// <summary>Gets optional source and target size information.</summary>
	public string? FileSizeText { get; }

	/// <summary>Gets a symbol that communicates status independently of color.</summary>
	public string StatusSymbol { get; }

	/// <summary>Gets the structured file status, or <see langword="null"/> for a summary row.</summary>
	public ConversionStatus? Status { get; }

	/// <summary>Gets a value indicating whether file-size information is available.</summary>
	public bool HasFileSizeText => !string.IsNullOrEmpty(FileSizeText);

	/// <summary>Gets a value indicating whether this is the final summary row.</summary>
	public bool IsSummary { get; }

	/// <summary>Gets a value indicating whether this row represents a successful conversion.</summary>
	public bool IsSuccessful => Status is ConversionStatus.Converted or ConversionStatus.Overwritten;

	/// <summary>Gets a value indicating whether this row represents a skipped file.</summary>
	public bool IsSkipped => Status == ConversionStatus.Skipped;

	/// <summary>Gets a value indicating whether this row represents a failed file or run.</summary>
	public bool IsFailed => Status == ConversionStatus.Failed;

	/// <summary>
	/// Creates a localized history entry from a structured service progress update.
	/// </summary>
	public static ConversionHistoryEntry FromProgress(ImageConversionProgressUpdate update)
	{
		ArgumentNullException.ThrowIfNull(update);
		if (update.Status is null
			|| update.SourceFileName is null
			|| update.TargetFileName is null
			|| update.SourceFormat is null)
		{
			throw new ArgumentException(
				"A completed-file update is required to create a history entry.",
				nameof(update));
		}

		ConversionStatus status = update.Status.Value;

		string statusText = GetStatusText(status);
		string resultText = status is ConversionStatus.Converted or ConversionStatus.Overwritten
			? string.Format(
				CultureInfo.CurrentCulture,
				Strings.ConversionResult_Target,
				update.TargetFileName)
			: string.Format(
				CultureInfo.CurrentCulture,
				Strings.ConversionResult_Error,
				statusText,
				GetErrorText(update.ErrorKind));
		string detailText = string.Format(
			CultureInfo.CurrentCulture,
			Strings.ConversionResult_Formats,
			update.SourceFormat,
			update.TargetFormat);
		string? fileSizeText = status is ConversionStatus.Converted or ConversionStatus.Overwritten
			&& update.SourceFileSize.HasValue
			&& update.TargetFileSize.HasValue
			? $"{FormatFileSize(update.SourceFileSize.Value)} → {FormatFileSize(update.TargetFileSize.Value)}"
			: null;

		return new ConversionHistoryEntry(
			update.SourceFileName,
			resultText,
			detailText,
			fileSizeText,
			GetStatusSymbol(status),
			status,
			isSummary: false);
	}

	/// <summary>
	/// Creates the final localized summary row for a completed conversion run.
	/// </summary>
	public static ConversionHistoryEntry CreateSummary(ImageConvertResult result)
	{
		ArgumentNullException.ThrowIfNull(result);

		int converted = Math.Max(0, result.Processed - result.Overwritten);
		string details = string.Format(
			CultureInfo.CurrentCulture,
			Strings.Summary_Completed,
			converted,
			result.Overwritten,
			result.Skipped,
			result.Failed,
			result.SavedMB.ToString("F2", CultureInfo.CurrentCulture));

		return new ConversionHistoryEntry(
			Strings.Summary_Title,
			details,
			string.Empty,
			fileSizeText: null,
			"Σ",
			status: null,
			isSummary: true);
	}

	/// <summary>
	/// Creates the final summary row when a batch cannot be completed.
	/// </summary>
	public static ConversionHistoryEntry CreateFailedSummary(string errorMessage)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

		return new ConversionHistoryEntry(
			Strings.Summary_Title,
			string.Format(
				CultureInfo.CurrentCulture,
				Strings.ConversionError_BatchFailed,
				errorMessage),
			string.Empty,
			fileSizeText: null,
			"!",
			ConversionStatus.Failed,
			isSummary: true);
	}

	static string GetStatusText(ConversionStatus status)
	{
		return status switch
		{
			ConversionStatus.Converted => Strings.ConversionStatus_Converted,
			ConversionStatus.Skipped => Strings.ConversionStatus_Skipped,
			ConversionStatus.Overwritten => Strings.ConversionStatus_Overwritten,
			ConversionStatus.Failed => Strings.ConversionStatus_Failed,
			_ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
		};
	}

	static string GetStatusSymbol(ConversionStatus status)
	{
		return status switch
		{
			ConversionStatus.Converted or ConversionStatus.Overwritten => "✓",
			ConversionStatus.Skipped => "–",
			ConversionStatus.Failed => "!",
			_ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
		};
	}

	static string GetErrorText(ConversionErrorKind errorKind)
	{
		return errorKind switch
		{
			ConversionErrorKind.TargetExists => Strings.ConversionError_TargetExists,
			ConversionErrorKind.SourceMatchesTarget => Strings.ConversionError_SourceMatchesTarget,
			ConversionErrorKind.UnsupportedFormat => Strings.ConversionError_UnsupportedFormat,
			ConversionErrorKind.DecodeFailed => Strings.ConversionError_DecodeFailed,
			ConversionErrorKind.EncodeFailed => Strings.ConversionError_EncodeFailed,
			ConversionErrorKind.IoError => Strings.ConversionError_Io,
			ConversionErrorKind.AccessDenied => Strings.ConversionError_AccessDenied,
			_ => Strings.ConversionError_Generic
		};
	}

	static string FormatFileSize(long bytes)
	{
		const double bytesPerKilobyte = 1024;
		const double bytesPerMegabyte = bytesPerKilobyte * 1024;

		return bytes switch
		{
			>= (long)bytesPerMegabyte =>
				$"{(bytes / bytesPerMegabyte).ToString("0.##", CultureInfo.CurrentCulture)} MB",
			>= (long)bytesPerKilobyte =>
				$"{(bytes / bytesPerKilobyte).ToString("0.#", CultureInfo.CurrentCulture)} KB",
			_ => $"{bytes.ToString(CultureInfo.CurrentCulture)} B"
		};
	}
}

#pragma warning restore CA1863
