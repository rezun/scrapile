using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Scrapile.Desktop.ViewModels;

namespace Scrapile.Desktop.Views;

public partial class TabListView : UserControl, INotifyPropertyChanged
{
    /// <summary>
    /// Private drag format for tab reordering. The payload is the TabId as a
    /// string; the view model is responsible for mapping it back to a tab.
    /// </summary>
    private static readonly DataFormat<string> TabDragFormat =
        DataFormat.CreateStringApplicationFormat("scrapile.tabitem");

    private const double DragThreshold = 4.0;

    private bool _isScrollable;
    private TabItemViewModel? _pendingDragTab;
    private Point? _pointerPressPosition;
    // Captured so DoDragDropAsync (which now requires PointerPressedEventArgs in
    // Avalonia 12) can be invoked once the pointer has crossed the drag threshold.
    private PointerPressedEventArgs? _pendingDragPressArgs;

    public new event PropertyChangedEventHandler? PropertyChanged;

    public bool IsScrollable
    {
        get => _isScrollable;
        private set
        {
            if (_isScrollable != value)
            {
                _isScrollable = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsScrollable)));
            }
        }
    }

    public TabListView()
    {
        InitializeComponent();

        // Set shortcut hint based on platform
        var shortcut = OperatingSystem.IsMacOS() ? "⌘T" : "Ctrl+T";
        if (this.FindControl<TextBlock>("InlineShortcutHint") is { } hint)
            hint.Text = shortcut;
        if (this.FindControl<TextBlock>("FixedShortcutHint") is { } fixedHint)
            fixedHint.Text = shortcut;

        // Subscribe to drag events at the user control level so tab targets
        // can be resolved from the bubbled event source.
        AddHandler(DragDrop.DragOverEvent, OnTabDragOver);
        AddHandler(DragDrop.DropEvent, OnTabDrop);
    }

    /// <summary>
    /// Handles scroll changes to detect when tabs overflow and scrolling is needed.
    /// </summary>
    private void OnTabScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (sender is ScrollViewer scrollViewer)
        {
            IsScrollable = scrollViewer.Extent.Height > scrollViewer.Viewport.Height;
        }
    }

    /// <summary>
    /// Handles double-tap on the tab area to create a new tab.
    /// </summary>
    private void OnTabAreaDoubleTapped(object? sender, TappedEventArgs e)
    {
        // Only create tab if double-click was on empty space (not on a tab item)
        if (DataContext is TabListViewModel vm)
        {
            var source = e.Source as Control;
            while (source != null && source != this)
            {
                if (source.DataContext is TabItemViewModel)
                    return; // Clicked on a tab, don't create new
                source = source.Parent as Control;
            }
            vm.CreateNewTabCommand.Execute(null);
        }
    }

    /// <summary>
    /// Handles click on the new tab button (both inline and fixed).
    /// </summary>
    private void OnNewTabButtonPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is TabListViewModel vm)
        {
            vm.CreateNewTabCommand.Execute(null);
        }
    }

    /// <summary>
    /// Handles pointer press on a tab item to select it and prime a potential drag.
    /// </summary>
    private void OnTabPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Border border || border.DataContext is not TabItemViewModel tabViewModel)
            return;

        if (DataContext is TabListViewModel listViewModel)
        {
            listViewModel.SelectTab(tabViewModel);
        }

        // Only prime a drag for left-button presses; right-click opens the context menu.
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _pendingDragTab = tabViewModel;
            _pointerPressPosition = e.GetPosition(this);
            _pendingDragPressArgs = e;
        }
    }

    /// <summary>
    /// Handles pointer movement on a tab item. Initiates a drag-and-drop reorder
    /// once the pointer has moved far enough from the press position.
    /// </summary>
    private async void OnTabPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_pendingDragTab is null || _pointerPressPosition is null || _pendingDragPressArgs is null)
            return;

        var currentPoint = e.GetCurrentPoint(this);
        if (!currentPoint.Properties.IsLeftButtonPressed)
        {
            // Button released without crossing the threshold — reset.
            _pendingDragTab = null;
            _pointerPressPosition = null;
            _pendingDragPressArgs = null;
            return;
        }

        var delta = e.GetPosition(this) - _pointerPressPosition.Value;
        if (Math.Abs(delta.X) < DragThreshold && Math.Abs(delta.Y) < DragThreshold)
            return;

        var draggedTab = _pendingDragTab;
        var pressArgs = _pendingDragPressArgs;
        _pendingDragTab = null;
        _pointerPressPosition = null;
        _pendingDragPressArgs = null;

        var dataTransfer = new DataTransfer();
        dataTransfer.Add(DataTransferItem.Create(TabDragFormat, draggedTab.TabId.ToString()));

        try
        {
            await DragDrop.DoDragDropAsync(pressArgs, dataTransfer, DragDropEffects.Move);
        }
        catch (Exception)
        {
            // Swallow — a failed drag should not crash the UI.
        }

        // Persist whatever order the live-reorder left us in (drop or cancel).
        if (DataContext is TabListViewModel vm)
        {
            await vm.PersistCurrentTabOrderAsync();
        }
    }

    /// <summary>
    /// Resets the drag-priming state if the pointer is released without a drag starting.
    /// </summary>
    private void OnTabPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _pendingDragTab = null;
        _pointerPressPosition = null;
        _pendingDragPressArgs = null;
    }

    /// <summary>
    /// Handles drag-over on the tab list. Performs live reordering so the user sees
    /// tabs shuffle in place as they drag.
    /// </summary>
    private void OnTabDragOver(object? sender, DragEventArgs e)
    {
        if (e.DataTransfer is null || !e.DataTransfer.Contains(TabDragFormat))
        {
            e.DragEffects = DragDropEffects.None;
            return;
        }

        e.DragEffects = DragDropEffects.Move;

        var draggedIdString = e.DataTransfer.TryGetValue(TabDragFormat);
        if (draggedIdString is null || !Guid.TryParse(draggedIdString, out var draggedTabId))
            return;
        if (DataContext is not TabListViewModel vm)
            return;

        var dragged = FindTabById(vm, draggedTabId);
        if (dragged is null)
            return;

        var target = FindTabFromVisual(e.Source as Control);
        if (target is null || ReferenceEquals(target, dragged))
            return;

        var oldIndex = vm.Tabs.IndexOf(dragged);
        var newIndex = vm.Tabs.IndexOf(target);
        if (oldIndex < 0 || newIndex < 0 || oldIndex == newIndex)
            return;

        vm.Tabs.Move(oldIndex, newIndex);
    }

    /// <summary>
    /// Drop handler — persistence is actually done after DoDragDropAsync returns,
    /// but we still acknowledge the drop so the effect is correct.
    /// </summary>
    private void OnTabDrop(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.DataTransfer is not null && e.DataTransfer.Contains(TabDragFormat)
            ? DragDropEffects.Move
            : DragDropEffects.None;
    }

    private static TabItemViewModel? FindTabById(TabListViewModel vm, Guid tabId)
    {
        foreach (var tab in vm.Tabs)
        {
            if (tab.TabId == tabId)
                return tab;
        }
        return null;
    }

    /// <summary>
    /// Walks up the visual tree from the drag event's source to find the
    /// TabItemViewModel the pointer is currently over.
    /// </summary>
    private TabItemViewModel? FindTabFromVisual(Control? source)
    {
        while (source is not null && source != this)
        {
            if (source.DataContext is TabItemViewModel tab)
                return tab;
            source = source.Parent as Control;
        }
        return null;
    }

    /// <summary>
    /// Gets the TabItemViewModel from a context menu event sender.
    /// The DataContext is inherited from the Border through the MenuFlyout.
    /// </summary>
    private TabItemViewModel? GetTabFromContextMenu(object? sender)
    {
        if (sender is MenuItem menuItem && menuItem.DataContext is TabItemViewModel tabViewModel)
        {
            return tabViewModel;
        }
        return null;
    }

    /// <summary>
    /// Handles the "Close Tab" context menu item click.
    /// </summary>
    private async void OnContextMenuClose(object? sender, RoutedEventArgs e)
    {
        var tabViewModel = GetTabFromContextMenu(sender);
        if (tabViewModel != null && DataContext is TabListViewModel listViewModel)
        {
            await listViewModel.CloseTabAsync(tabViewModel);
        }
    }

    /// <summary>
    /// Handles the "Close All Tabs" context menu item click.
    /// </summary>
    private async void OnContextMenuCloseAll(object? sender, RoutedEventArgs e)
    {
        if (DataContext is TabListViewModel listViewModel)
        {
            await listViewModel.CloseAllTabsAsync();
        }
    }

    /// <summary>
    /// Handles the "Close Tabs Above" context menu item click.
    /// </summary>
    private async void OnContextMenuCloseAbove(object? sender, RoutedEventArgs e)
    {
        var tabViewModel = GetTabFromContextMenu(sender);
        if (tabViewModel != null && DataContext is TabListViewModel listViewModel)
        {
            await listViewModel.CloseTabsAboveAsync(tabViewModel);
        }
    }

    /// <summary>
    /// Handles the "Close Tabs Below" context menu item click.
    /// </summary>
    private async void OnContextMenuCloseBelow(object? sender, RoutedEventArgs e)
    {
        var tabViewModel = GetTabFromContextMenu(sender);
        if (tabViewModel != null && DataContext is TabListViewModel listViewModel)
        {
            await listViewModel.CloseTabsBelowAsync(tabViewModel);
        }
    }

    /// <summary>
    /// Handles the "Duplicate Tab" context menu item click.
    /// </summary>
    private async void OnContextMenuDuplicate(object? sender, RoutedEventArgs e)
    {
        var tabViewModel = GetTabFromContextMenu(sender);
        if (tabViewModel != null && DataContext is TabListViewModel listViewModel)
        {
            await listViewModel.DuplicateTabAsync(tabViewModel);
        }
    }

    /// <summary>
    /// Handles the "Edit Title" context menu item click.
    /// </summary>
    private void OnContextMenuEditTitle(object? sender, RoutedEventArgs e)
    {
        var tabViewModel = GetTabFromContextMenu(sender);
        if (tabViewModel != null && DataContext is TabListViewModel listViewModel)
        {
            // First select the tab, then request edit title
            listViewModel.SelectTab(tabViewModel);
            listViewModel.RequestEditTitle();
        }
    }

    /// <summary>
    /// Handles the "Copy to Clipboard" context menu item click.
    /// </summary>
    private void OnContextMenuCopyToClipboard(object? sender, RoutedEventArgs e)
    {
        var tabViewModel = GetTabFromContextMenu(sender);
        if (tabViewModel != null && DataContext is TabListViewModel listViewModel)
        {
            listViewModel.RequestCopyToClipboard(tabViewModel);
        }
    }

    /// <summary>
    /// Handles the "Save As..." context menu item click.
    /// </summary>
    private void OnContextMenuSaveAs(object? sender, RoutedEventArgs e)
    {
        var tabViewModel = GetTabFromContextMenu(sender);
        if (tabViewModel != null && DataContext is TabListViewModel listViewModel)
        {
            listViewModel.RequestSaveAs(tabViewModel);
        }
    }

    /// <summary>
    /// Handles the "Delete" context menu item click.
    /// </summary>
    private void OnContextMenuDelete(object? sender, RoutedEventArgs e)
    {
        var tabViewModel = GetTabFromContextMenu(sender);
        if (tabViewModel != null && DataContext is TabListViewModel listViewModel)
        {
            listViewModel.RequestDelete(tabViewModel);
        }
    }
}
