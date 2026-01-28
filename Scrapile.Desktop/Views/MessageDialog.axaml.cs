using System;
using Avalonia.Controls;
using Avalonia.Input;
using Scrapile.Desktop.ViewModels;

namespace Scrapile.Desktop.Views;

/// <summary>
/// A reusable message dialog window with configurable buttons.
/// </summary>
public partial class MessageDialog : Window
{
    /// <summary>
    /// The result of the dialog interaction.
    /// </summary>
    public MessageDialogResult Result { get; private set; } = MessageDialogResult.Cancelled;

    public MessageDialog()
    {
        InitializeComponent();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            Result = MessageDialogResult.Cancelled;
            Close();
        }
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is MessageDialogViewModel viewModel)
        {
            viewModel.CloseRequested += OnCloseRequested;
        }
    }

    private void OnCloseRequested(object? sender, MessageDialogResult result)
    {
        Result = result;
        Close();
    }
}
