namespace Scrapile.Desktop.Services;

using System;
using System.Threading;
using System.Threading.Tasks;

public interface IAppUpdateService
{
    event EventHandler? StateChanged;

    bool IsUpdateReadyToInstall { get; }
    string UpdateButtonText { get; }

    Task StartAsync(CancellationToken cancellationToken);
    void ApplyPendingUpdateAndRestart();
    void ShowTestUpdateNotification();
}
