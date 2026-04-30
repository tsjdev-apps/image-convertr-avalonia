using Avalonia;

namespace ImageConvertr.App;

/// <summary>
/// The application entry point. 
/// Initializes and starts the Avalonia application.
/// </summary>
sealed class Program
{
	/// <summary>
	/// Starts the application after Avalonia has been configured.
	/// </summary>
	/// <param name="args">The command-line arguments passed to the application.</param>
	[STAThread]
	public static void Main(string[] args) 
		=> BuildAvaloniaApp()
			.StartWithClassicDesktopLifetime(args);

	/// <summary>
	/// Builds the Avalonia application configuration used by runtime startup and the visual designer.
	/// </summary>
	/// <returns>The configured Avalonia application builder.</returns>
	public static AppBuilder BuildAvaloniaApp()
		=> AppBuilder.Configure<App>()
			.UsePlatformDetect()
			.WithInterFont()
			.LogToTrace();
}
