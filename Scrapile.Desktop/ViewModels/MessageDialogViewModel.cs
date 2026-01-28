namespace Scrapile.Desktop.ViewModels;

using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

/// <summary>
/// Result of a message dialog interaction.
/// </summary>
public enum MessageDialogResult
{
    Cancelled,
    Primary,
    Secondary,
    Tertiary
}

/// <summary>
/// ViewModel for a reusable message dialog with configurable buttons.
/// </summary>
public partial class MessageDialogViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _message = string.Empty;

    [ObservableProperty]
    private string _primaryButtonText = "OK";

    [ObservableProperty]
    private string? _secondaryButtonText;

    [ObservableProperty]
    private string? _tertiaryButtonText;

    /// <summary>
    /// Whether the secondary button should be shown.
    /// </summary>
    public bool HasSecondaryButton => !string.IsNullOrEmpty(SecondaryButtonText);

    /// <summary>
    /// Whether the tertiary button should be shown.
    /// </summary>
    public bool HasTertiaryButton => !string.IsNullOrEmpty(TertiaryButtonText);

    /// <summary>
    /// Event raised when the dialog should close with a result.
    /// </summary>
    public event EventHandler<MessageDialogResult>? CloseRequested;

    partial void OnSecondaryButtonTextChanged(string? value)
    {
        OnPropertyChanged(nameof(HasSecondaryButton));
    }

    partial void OnTertiaryButtonTextChanged(string? value)
    {
        OnPropertyChanged(nameof(HasTertiaryButton));
    }

    [RelayCommand]
    private void Primary()
    {
        CloseRequested?.Invoke(this, MessageDialogResult.Primary);
    }

    [RelayCommand]
    private void Secondary()
    {
        CloseRequested?.Invoke(this, MessageDialogResult.Secondary);
    }

    [RelayCommand]
    private void Tertiary()
    {
        CloseRequested?.Invoke(this, MessageDialogResult.Tertiary);
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke(this, MessageDialogResult.Cancelled);
    }
}
