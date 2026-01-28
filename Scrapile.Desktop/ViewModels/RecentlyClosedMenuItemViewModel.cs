namespace Scrapile.Desktop.ViewModels;

using System;
using CommunityToolkit.Mvvm.Input;
using Scrapile.Application.DTOs;

/// <summary>
/// ViewModel for a recently closed item in the menu bar submenu.
/// </summary>
public partial class RecentlyClosedMenuItemViewModel : ViewModelBase
{
    private readonly Action<Guid> _reopenCallback;

    /// <summary>
    /// The document ID.
    /// </summary>
    public Guid DocumentId { get; }

    /// <summary>
    /// The menu header text (formatted: "Title    (2 min ago)").
    /// </summary>
    public string MenuHeader { get; }

    /// <summary>
    /// Creates a RecentlyClosedMenuItemViewModel from a RecentlyClosedItem.
    /// </summary>
    /// <param name="item">The recently closed item data.</param>
    /// <param name="reopenCallback">Callback when reopen is requested (receives document ID).</param>
    public RecentlyClosedMenuItemViewModel(RecentlyClosedItem item, Action<Guid> reopenCallback)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(reopenCallback);

        _reopenCallback = reopenCallback;
        DocumentId = item.DocumentId;

        // Format: "Title    (2 min ago)" with spacing for visual separation
        var displayName = item.DisplayName;
        // Truncate long titles to keep menu items reasonable
        if (displayName.Length > 40)
        {
            displayName = displayName[..37] + "...";
        }

        MenuHeader = $"{displayName}    ({item.FormattedClosedTime})";
    }

    /// <summary>
    /// Command to reopen this document.
    /// </summary>
    [RelayCommand]
    private void Reopen()
    {
        _reopenCallback(DocumentId);
    }
}
