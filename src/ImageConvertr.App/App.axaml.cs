using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ImageConvertr.App.ViewModels;
using ImageConvertr.App.Views;
using ImageConvertr.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ImageConvertr.App;

/// <summary>
/// Configures application services and desktop lifetime behavior.
/// </summary>
public partial class App : Application
{
	/// <summary>
	/// The application's service provider for resolving dependencies.
	/// </summary>
	ServiceProvider? serviceProvider;

	/// <summary>
	/// Initializes the Avalonia application and loads XAML resources.
	/// </summary>
	public override void Initialize()
	{
		AvaloniaXamlLoader.Load(this);
	}

	/// <summary>
	/// Called when the framework initialization is completed.
	/// Responsible for setting up services, 
	/// injecting the MainWindowViewModel, and attaching the 
	/// main window to the application lifetime.
	/// </summary>
	public override void OnFrameworkInitializationCompleted()
	{
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			serviceProvider = ConfigureServices();
			desktop.MainWindow = serviceProvider.GetRequiredService<MainWindow>();
			desktop.Exit += DesktopOnExit;
		}

		base.OnFrameworkInitializationCompleted();
	}

	/// <summary>
	/// Configures all application services and view models.
	/// </summary>
	/// <returns>The configured application service provider.</returns>
	static ServiceProvider ConfigureServices()
	{
		ServiceCollection services = new();

		_ = services.AddSingleton<IImageConvertrService, ImageConvertrService>();
		_ = services.AddSingleton<MainWindowViewModel>();
		_ = services.AddSingleton<MainWindow>();

		return services.BuildServiceProvider(new ServiceProviderOptions
		{
			ValidateOnBuild = true,
			ValidateScopes = true
		});
	}

	/// <summary>
	/// Disposes the application service provider when the desktop lifetime exits.
	/// </summary>
	void DesktopOnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
	{
		serviceProvider?.Dispose();
		serviceProvider = null;
	}
}
