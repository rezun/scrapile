using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Scrapile.Desktop.ViewModels;

namespace Scrapile.Desktop.Views;

/// <summary>
/// Settings window code-behind.
/// </summary>
public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is SettingsViewModel viewModel)
        {
            viewModel.CloseRequested += OnCloseRequested;
            viewModel.FolderBrowserRequested += OnFolderBrowserRequested;
            viewModel.Initialize();
        }
    }

    private void OnCloseRequested(object? sender, EventArgs e)
    {
        Close();
    }

    private async void OnFolderBrowserRequested(object? sender, FolderBrowserEventArgs e)
    {
        var viewModel = DataContext as SettingsViewModel;
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
            await viewModel.SetStorageDirectoryAsync(selectedPath);
        }
    }
}
