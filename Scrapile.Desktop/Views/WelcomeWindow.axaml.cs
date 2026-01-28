using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Scrapile.Desktop.ViewModels;

namespace Scrapile.Desktop.Views;

/// <summary>
/// First-run welcome window code-behind.
/// </summary>
public partial class WelcomeWindow : Window
{
    private TaskCompletionSource<string>? _completionSource;

    /// <summary>
    /// Gets the selected storage directory after the window closes.
    /// </summary>
    public string? SelectedStorageDirectory { get; private set; }

    public WelcomeWindow()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is WelcomeViewModel viewModel)
        {
            viewModel.FolderBrowserRequested += OnFolderBrowserRequested;
            viewModel.ContinueRequested += OnContinueRequested;
        }
    }

    /// <summary>
    /// Shows the welcome window and waits for the user to select a storage directory.
    /// </summary>
    /// <returns>The selected storage directory path.</returns>
    public Task<string> ShowAndGetResultAsync()
    {
        _completionSource = new TaskCompletionSource<string>();

        Closed += (_, _) =>
        {
            var viewModel = DataContext as WelcomeViewModel;
            var directory = SelectedStorageDirectory ?? viewModel?.StorageDirectory ?? string.Empty;
            _completionSource.TrySetResult(directory);
        };

        Show();

        return _completionSource.Task;
    }

    private async void OnFolderBrowserRequested(object? sender, FolderBrowserEventArgs e)
    {
        var viewModel = DataContext as WelcomeViewModel;
        if (viewModel == null) return;

        var options = new FolderPickerOpenOptions
        {
            Title = "Select Document Storage Directory",
            AllowMultiple = false
        };

        // Try to set initial folder if provided
        if (!string.IsNullOrEmpty(e.InitialDirectory) && Directory.Exists(e.InitialDirectory))
        {
            try
            {
                var folder = await StorageProvider.TryGetFolderFromPathAsync(e.InitialDirectory);
                if (folder != null)
                {
                    options.SuggestedStartLocation = folder;
                }
            }
            catch
            {
                // Ignore if we can't get the folder
            }
        }

        var result = await StorageProvider.OpenFolderPickerAsync(options);

        if (result.Count > 0)
        {
            var selectedPath = result[0].Path.LocalPath;
            viewModel.SetStorageDirectory(selectedPath);
        }
    }

    private void OnContinueRequested(object? sender, EventArgs e)
    {
        var viewModel = DataContext as WelcomeViewModel;
        SelectedStorageDirectory = viewModel?.StorageDirectory;
        Close();
    }
}
