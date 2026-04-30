using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using ImageConvertr.Core.Models;

namespace ImageConvertr.App.Converters;

/// <summary>
/// Converts log levels into brushes for status text.
/// </summary>
public sealed class LogLevelToBrushConverter : IValueConverter
{
	/// <summary>
	/// Gets the brush used for error messages.
	/// </summary>
	public static readonly IBrush ErrorBrush = Brushes.Red;

	/// <summary>
	/// Gets the brush used for success messages.
	/// </summary>
	public static readonly IBrush SuccessBrush = new SolidColorBrush(Color.Parse("#00BF63"));

	/// <summary>
	/// Gets the brush used for informational messages.
	/// </summary>
	public static readonly IBrush InfoBrush = Brushes.Gray;

	/// <summary>
	/// Converts a <see cref="LogLevel"/> value to an <see cref="IBrush"/>
	/// suitable for use as a <c>Foreground</c> color in the UI.
	/// </summary>
	/// <returns>The brush used to render the log level.</returns>
	public object? Convert(
		object? value, Type targetType,
		object? parameter, CultureInfo? culture)
	{
		if (value is LogLevel level)
		{
			return level switch
			{
				LogLevel.Error => ErrorBrush,
				LogLevel.Success => SuccessBrush,
				_ => InfoBrush,
			};
		}

		return InfoBrush;
	}

	/// <summary>
	/// Not supported; converter is one-way.
	/// </summary>
	/// <returns>This method always throws.</returns>
	public object? ConvertBack(
		object? value, Type targetType,
		object? parameter, CultureInfo? culture)
		=> throw new NotSupportedException();
}
