using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using System;
using System.Threading.Tasks;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Scrapile.Application.Helpers;
using Scrapile.Application.Services;
using Scrapile.Desktop.DependencyInjection;
using Scrapile.Desktop.ViewModels;
using Scrapile.Desktop.Views;
using Scrapile.Domain.Constants;
using Scrapile.Domain.Entities;
using Scrapile.Desktop.Services;
using Scrapile.Domain.Interfaces;
using Scrapile.Infrastructure.Services;
using Scrapile.Infrastructure.Storage;

namespace Scrapile.Desktop;

public partial class App : Avalonia.Application
{
    /// <summary>
    /// Gets the service provider for dependency injection.
    /// </summary>
    public IServiceProvider? Services { get; private set; }

    /// <summary>
    /// Gets the storage lock service that prevents multiple instances.
    /// </summary>
    private IStorageLockService? _storageLockService;

    /// <summary>
    /// Gets the main window reference.
    /// </summary>
    public MainWindow? MainWindow { get; private set; }

    /// <summary>
    /// Gets whether the application is in the process of quitting.
    /// Used to prevent the window closing handler from canceling the close.
    /// </summary>
    public bool IsQuitting { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (TryGetFeature(typeof(IActivatableLifetime)) is IActivatableLifetime activatable)
        {
            activatable.Activated += OnAppActivated;
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Schedule async initialization on UI thread after framework is ready
            Dispatcher.UIThread.InvokeAsync(() => InitializeApplicationAsync(desktop));
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnAppActivated(object? sender, ActivatedEventArgs e)
    {
        if (MainWindow is null || IsQuitting)
        {
            return;
        }

        Dispatcher.UIThread.Post(ShowWindow);
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
            }
            else
            {
                // Existing user - load configured directory
                storageDirectory = ServiceCollectionExtensions.GetStorageDirectory();
            }

            // Try to acquire exclusive lock on the storage directory
            _storageLockService = new StorageLockService();
            if (!_storageLockService.TryAcquireLock(storageDirectory))
            {
                var lockInfo = _storageLockService.GetExistingLockInfo(storageDirectory);
                await ShowStorageLockErrorAsync(storageDirectory, lockInfo);
                desktop.Shutdown(1);
                return;
            }

            // Configure dependency injection
            var services = new ServiceCollection();
            services.AddScrapileServices(storageDirectory);
            Services = services.BuildServiceProvider();

            var updateService = Services.GetRequiredService<IAppUpdateService>();
            _ = updateService.StartAsync(CancellationToken.None);

            desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;

            // Create main window with DI-resolved view model
            var viewModel = Services.GetRequiredService<MainWindowViewModel>();
            MainWindow = new MainWindow
            {
                DataContext = viewModel,
            };
            desktop.MainWindow = MainWindow;
            desktop.MainWindow.Show();
            desktop.MainWindow.Activate();

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

    private async Task ShowStorageLockErrorAsync(string storageDirectory, LockInfo? lockInfo)
    {
        var message = $"Another instance of Scrapile is already using this storage folder:\n\n{storageDirectory}";

        if (lockInfo != null)
        {
            message += $"\n\nProcess ID: {lockInfo.Pid}";
            if (!string.IsNullOrEmpty(lockInfo.MachineName))
            {
                message += $"\nMachine: {lockInfo.MachineName}";
            }
        }

        message += "\n\nPlease close the other instance or choose a different storage folder.";

        var viewModel = new MessageDialogViewModel
        {
            Title = "Scrapile Already Running",
            Message = message,
            PrimaryButtonText = "OK"
        };

        var dialog = new MessageDialog { DataContext = viewModel };

        // Use Show() since there's no owner window yet, then wait for close
        var tcs = new TaskCompletionSource<bool>();
        dialog.Closed += (_, _) => tcs.TrySetResult(true);
        dialog.Show();
        await tcs.Task;
    }

    /// <summary>
    /// Shows the main window and brings it to front.
    /// </summary>
    public void ShowWindow()
    {
        if (MainWindow == null)
        {
            return;
        }

        // Show dock icon first on macOS so the window appears properly
        MacOSDockService.ShowDockIcon();

        MainWindow.Show();
        MainWindow.WindowState = WindowState.Normal;
        MainWindow.Activate();
    }

    /// <summary>
    /// Quits the application gracefully.
    /// </summary>
    public void QuitApplication()
    {
        if (IsQuitting)
        {
            return;
        }

        IsQuitting = true;

        // Post shutdown to dispatcher so it happens after the current event is fully processed.
        // This avoids freezing when called from native menu handlers.
        Dispatcher.UIThread.Post(() =>
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
        }, DispatcherPriority.Background);
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

        // Release storage lock
        _storageLockService?.ReleaseLock();

        // Dispose of services that implement IDisposable
        if (Services is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

}
