using System.Globalization;
using ImageConvertr.App.Resources.Localization;

namespace ImageConvertr.Tests;

/// <summary>
/// Verifies English fallback and German resource resolution.
/// </summary>
public sealed class LocalizationTests
{
	/// <summary>
	/// Verifies that German regional cultures use the German resources.
	/// </summary>
	[Theory]
	[InlineData("de-DE")]
	[InlineData("de-AT")]
	[InlineData("de-CH")]
	public void GermanCulturesResolveGermanResources(string cultureName)
	{
		CultureInfo culture = CultureInfo.GetCultureInfo(cultureName);

		Assert.Equal(
			"Quellordner",
			Strings.GetForCulture(nameof(Strings.SourceFolder_Label), culture));
		Assert.Equal(
			"Es wurden noch keine Dateien verarbeitet.",
			Strings.GetForCulture(nameof(Strings.Progress_NoFiles), culture));
	}

	/// <summary>
	/// Verifies that English regional cultures use the neutral English resources.
	/// </summary>
	[Theory]
	[InlineData("en-US")]
	[InlineData("en-GB")]
	public void EnglishCulturesResolveEnglishResources(string cultureName)
	{
		CultureInfo culture = CultureInfo.GetCultureInfo(cultureName);

		Assert.Equal(
			"Source folder",
			Strings.GetForCulture(nameof(Strings.SourceFolder_Label), culture));
		Assert.Equal(
			"No files have been processed yet.",
			Strings.GetForCulture(nameof(Strings.Progress_NoFiles), culture));
	}

	/// <summary>
	/// Verifies that unsupported cultures fall back to neutral English.
	/// </summary>
	[Theory]
	[InlineData("fr-FR")]
	[InlineData("es-ES")]
	[InlineData("it-IT")]
	public void UnsupportedCulturesFallBackToEnglish(string cultureName)
	{
		CultureInfo culture = CultureInfo.GetCultureInfo(cultureName);

		Assert.Equal(
			"Convert images",
			Strings.GetForCulture(nameof(Strings.ConvertImages_Button), culture));
	}

	/// <summary>
	/// Verifies localized singular and plural progress patterns.
	/// </summary>
	[Fact]
	public void ProgressPatternsAreLocalized()
	{
		CultureInfo english = CultureInfo.GetCultureInfo("en-US");
		CultureInfo german = CultureInfo.GetCultureInfo("de-DE");

		Assert.Equal(
			"{0} / {1} file processed",
			Strings.GetForCulture(nameof(Strings.Progress_ProcessedFile), english));
		Assert.Equal(
			"{0} / {1} files processed",
			Strings.GetForCulture(nameof(Strings.Progress_ProcessedFiles), english));
		Assert.Equal(
			"{0} / {1} Datei verarbeitet",
			Strings.GetForCulture(nameof(Strings.Progress_ProcessedFile), german));
		Assert.Equal(
			"{0} / {1} Dateien verarbeitet",
			Strings.GetForCulture(nameof(Strings.Progress_ProcessedFiles), german));
	}
}
