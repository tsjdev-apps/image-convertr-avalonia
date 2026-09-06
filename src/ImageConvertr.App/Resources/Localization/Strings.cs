using System.Globalization;
using System.Resources;

namespace ImageConvertr.App.Resources.Localization;

#pragma warning disable CA1707, CS1591

/// <summary>
/// Provides strongly named access to the application's localized resources.
/// </summary>
public static class Strings
{
	static readonly ResourceManager resourceManager = new(
		"ImageConvertr.App.Resources.Localization.Strings",
		typeof(Strings).Assembly);

	/// <summary>
	/// Gets a localized value for tests and reusable formatting helpers.
	/// </summary>
	public static string GetForCulture(string key, CultureInfo culture)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(key);
		ArgumentNullException.ThrowIfNull(culture);

		return resourceManager.GetString(key, culture) ?? key;
	}

	static string Get(string key)
		=> resourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key;

	public static string UiLocale =>
		CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals(
			"de",
			StringComparison.OrdinalIgnoreCase)
			? "de-DE"
			: "en-US";

	public static string Window_Title => Get(nameof(Window_Title));
	public static string Main_Title => Get(nameof(Main_Title));
	public static string Main_Description => Get(nameof(Main_Description));
	public static string SourceFolder_Label => Get(nameof(SourceFolder_Label));
	public static string SourceFolder_Watermark => Get(nameof(SourceFolder_Watermark));
	public static string SourceFolder_PickerTitle => Get(nameof(SourceFolder_PickerTitle));
	public static string TargetFolder_Label => Get(nameof(TargetFolder_Label));
	public static string TargetFolder_Watermark => Get(nameof(TargetFolder_Watermark));
	public static string TargetFolder_PickerTitle => Get(nameof(TargetFolder_PickerTitle));
	public static string Browse_ToolTip => Get(nameof(Browse_ToolTip));
	public static string TargetFormat_Label => Get(nameof(TargetFormat_Label));
	public static string ImageQuality_Label => Get(nameof(ImageQuality_Label));
	public static string ImageQuality_Description => Get(nameof(ImageQuality_Description));
	public static string OverwriteFiles_Label => Get(nameof(OverwriteFiles_Label));
	public static string OverwriteFiles_Description => Get(nameof(OverwriteFiles_Description));
	public static string ConvertImages_Button => Get(nameof(ConvertImages_Button));
	public static string ConvertingImages_Button => Get(nameof(ConvertingImages_Button));
	public static string Validation_SourceFolderRequired => Get(nameof(Validation_SourceFolderRequired));
	public static string Validation_TargetFolderRequired => Get(nameof(Validation_TargetFolderRequired));
	public static string Progress_Title => Get(nameof(Progress_Title));
	public static string Progress_ProcessedFile => Get(nameof(Progress_ProcessedFile));
	public static string Progress_ProcessedFiles => Get(nameof(Progress_ProcessedFiles));
	public static string Progress_HistoryTitle => Get(nameof(Progress_HistoryTitle));
	public static string Progress_NoFiles => Get(nameof(Progress_NoFiles));
	public static string ConversionStatus_Converted => Get(nameof(ConversionStatus_Converted));
	public static string ConversionStatus_Skipped => Get(nameof(ConversionStatus_Skipped));
	public static string ConversionStatus_Overwritten => Get(nameof(ConversionStatus_Overwritten));
	public static string ConversionStatus_Failed => Get(nameof(ConversionStatus_Failed));
	public static string ConversionResult_Target => Get(nameof(ConversionResult_Target));
	public static string ConversionResult_Formats => Get(nameof(ConversionResult_Formats));
	public static string ConversionResult_Error => Get(nameof(ConversionResult_Error));
	public static string ConversionError_TargetExists => Get(nameof(ConversionError_TargetExists));
	public static string ConversionError_SourceMatchesTarget => Get(nameof(ConversionError_SourceMatchesTarget));
	public static string ConversionError_UnsupportedFormat => Get(nameof(ConversionError_UnsupportedFormat));
	public static string ConversionError_DecodeFailed => Get(nameof(ConversionError_DecodeFailed));
	public static string ConversionError_EncodeFailed => Get(nameof(ConversionError_EncodeFailed));
	public static string ConversionError_Io => Get(nameof(ConversionError_Io));
	public static string ConversionError_AccessDenied => Get(nameof(ConversionError_AccessDenied));
	public static string ConversionError_Generic => Get(nameof(ConversionError_Generic));
	public static string ConversionError_InvalidPath => Get(nameof(ConversionError_InvalidPath));
	public static string ConversionError_BatchFailed => Get(nameof(ConversionError_BatchFailed));
	public static string Summary_Title => Get(nameof(Summary_Title));
	public static string Summary_Completed => Get(nameof(Summary_Completed));
}

#pragma warning restore CA1707, CS1591
