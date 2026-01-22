using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System;
using System.Linq;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Scrapile.Application.Helpers;
using Scrapile.Desktop.DependencyInjection;
using Scrapile.Desktop.ViewModels;
using Scrapile.Desktop.Views;

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

            // Configure dependency injection
            var services = new ServiceCollection();
            var storageDirectory = ServiceCollectionExtensions.GetDefaultStorageDirectory();
            services.AddScrapileServices(storageDirectory);
            Services = services.BuildServiceProvider();

            // Create main window with DI-resolved view model
            var viewModel = Services.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = new MainWindow
            {
                DataContext = viewModel,
            };

            // Handle application shutdown
            desktop.ShutdownRequested += OnShutdownRequested;
        }

        base.OnFrameworkInitializationCompleted();
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
