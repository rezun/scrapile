namespace Scrapile.Desktop.ViewModels;

using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Scrapile.Application.DTOs;

/// <summary>
/// ViewModel for a recently closed item in the recently closed panel.
/// </summary>
public partial class RecentlyClosedItemViewModel : ViewModelBase
{
    private readonly Action<RecentlyClosedItemViewModel> _reopenCallback;

    /// <summary>
    /// The document ID.
    /// </summary>
    public Guid DocumentId { get; }

    /// <summary>
    /// The display name (title if set, otherwise content preview).
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Whether the document has a title.
    /// </summary>
    public bool HasTitle { get; }

    /// <summary>
    /// Formatted time since closed (e.g., "2 minutes ago").
    /// </summary>
    public string FormattedClosedTime { get; }

    /// <summary>
    /// Whether the document has been deleted from disk.
    /// </summary>
    public bool IsDeleted { get; }

    /// <summary>
    /// Creates a RecentlyClosedItemViewModel from a RecentlyClosedItem.
    /// </summary>
    /// <param name="item">The recently closed item data.</param>
    /// <param name="reopenCallback">Callback when reopen is requested.</param>
    public RecentlyClosedItemViewModel(RecentlyClosedItem item, Action<RecentlyClosedItemViewModel> reopenCallback)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(reopenCallback);

        _reopenCallback = reopenCallback;
        DocumentId = item.DocumentId;
        DisplayName = item.DisplayName;
        HasTitle = !string.IsNullOrWhiteSpace(item.Title);
        FormattedClosedTime = item.FormattedClosedTime;
        IsDeleted = item.IsDeleted;
    }

    /// <summary>
    /// Command to reopen this document.
    /// </summary>
    [RelayCommand]
    private void Reopen()
    {
        if (!IsDeleted)
        {
            _reopenCallback(this);
        }
    }
}
