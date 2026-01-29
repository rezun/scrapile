using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Scrapile.Application.Helpers;
using Scrapile.Desktop.DependencyInjection;
using Scrapile.Desktop.ViewModels;
using Scrapile.Desktop.Views;
using Scrapile.Domain.Entities;
using Scrapile.Desktop.Services;
using Scrapile.Infrastructure.Storage;

namespace Scrapile.Desktop;

public partial class App : Avalonia.Application
{
    /// <summary>
    /// Gets the service provider for dependency injection.
    /// </summary>
    public IServiceProvider? Services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();

            // Schedule async initialization on UI thread after framework is ready
            Dispatcher.UIThread.InvokeAsync(() => InitializeApplicationAsync(desktop));
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async Task InitializeApplicationAsync(IClassicDesktopStyleApplicationLifetime desktop)
    {
        try
        {
            var settingsStore = new JsonSettingsStore();
            string storageDirectory;

            if (!settingsStore.SettingsFileExists())
            {
                // Prevent app from shutting down when welcome window closes
                desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

                // First run - show welcome window to let user choose storage directory
                var (selectedDirectory, autorunAtStartup) = await ShowWelcomeWindowAsync();
                storageDirectory = selectedDirectory;

                // Save initial settings with selected directory and autorun preference
                var settings = AppSettings.CreateDefault();
                settings.StorageDirectory = storageDirectory;
                settings.AutorunAtStartup = autorunAtStartup;
                await settingsStore.SaveAsync(settings);

                // Register autorun if enabled
                if (autorunAtStartup)
                {
                    var autorunService = new AutorunService();
                    autorunService.SetAutorunEnabled(true);
                }

                // Restore normal shutdown behavior
                desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;
            }
            else
            {
                // Existing user - load configured directory
                storageDirectory = ServiceCollectionExtensions.GetStorageDirectory();
            }

            // Configure dependency injection
            var services = new ServiceCollection();
            services.AddScrapileServices(storageDirectory);
            Services = services.BuildServiceProvider();

            // Create main window with DI-resolved view model
            var viewModel = Services.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = new MainWindow
            {
                DataContext = viewModel,
            };
            desktop.MainWindow.Show();

            // Handle application shutdown
            desktop.ShutdownRequested += OnShutdownRequested;
        }
        catch (Exception ex)
        {
            // Log to stderr so the error is visible when running from terminal
            Console.Error.WriteLine($"Fatal error during application startup: {ex}");

            // Exit the application with error code
            desktop.Shutdown(1);
        }
    }

    private async Task<(string storageDirectory, bool autorunAtStartup)> ShowWelcomeWindowAsync()
    {
        var viewModel = new WelcomeViewModel();
        var welcomeWindow = new WelcomeWindow { DataContext = viewModel };

        var storageDirectory = await welcomeWindow.ShowAndGetResultAsync();

        // If empty (user closed without selecting), use default
        if (string.IsNullOrEmpty(storageDirectory))
        {
            storageDirectory = ServiceCollectionExtensions.GetDefaultStorageDirectory();
        }

        return (storageDirectory, welcomeWindow.AutorunAtStartup);
    }

    private void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
    {
        // Save all pending changes before shutting down
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
            desktop.MainWindow?.DataContext is MainWindowViewModel viewModel)
        {
            try
            {
                // Use AsyncHelper to run async save synchronously without deadlocks
                AsyncHelper.RunSync(viewModel.SaveAllPendingChangesAsync);
            }
            catch
            {
                // Don't prevent shutdown even if save fails
            }
        }

        // Dispose of services that implement IDisposable
        if (Services is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}
