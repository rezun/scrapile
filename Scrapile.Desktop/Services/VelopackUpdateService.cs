namespace Scrapile.Desktop.Services;

using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Velopack;
using Velopack.Sources;

public sealed class VelopackUpdateService : IAppUpdateService, IDisposable
{
    private static readonly TimeSpan DefaultCheckInterval = TimeSpan.FromHours(4);
    private static readonly TimeSpan MinimumCheckInterval = TimeSpan.FromMinutes(15);

    private readonly UpdateSettings _settings;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _checkGate = new(1, 1);
    private readonly Lock _startLock = new();

    private CancellationTokenSource? _lifetimeCts;
    private Task? _backgroundTask;
    private UpdateManager? _updateManager;
    private VelopackAsset? _pendingUpdate;
    private string? _testUpdateVersion;
    private bool _started;

    public event EventHandler? StateChanged;

    public bool IsUpdateReadyToInstall => _pendingUpdate is not null || _testUpdateVersion is not null;

    public string UpdateButtonText =>
        _pendingUpdate is not null
            ? $"Restart to install {_pendingUpdate.Version}"
            : _testUpdateVersion is not null
                ? $"Restart to install {_testUpdateVersion}"
                : string.Empty;

    public VelopackUpdateService(UpdateSettings settings, TimeProvider timeProvider)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        lock (_startLock)
        {
            if (_started)
                return Task.CompletedTask;

            _started = true;
        }

        if (IsDevelopmentEnvironment())
        {
            ShowTestUpdateNotification();
            return Task.CompletedTask;
        }

        if (!_settings.Enabled || string.IsNullOrWhiteSpace(_settings.GithubRepositoryUrl))
            return Task.CompletedTask;

        try
        {
            _updateManager = new UpdateManager(new GithubSource(_settings.GithubRepositoryUrl, null, false, null));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to initialize Velopack update manager: {ex}");
            return Task.CompletedTask;
        }

        if (_updateManager.CurrentVersion is null)
            return Task.CompletedTask;

        RefreshPendingUpdateState();

        _lifetimeCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _backgroundTask = RunUpdateLoopAsync(_lifetimeCts.Token);
        return Task.CompletedTask;
    }

    public void ApplyPendingUpdateAndRestart()
    {
        if (_pendingUpdate is null && _testUpdateVersion is not null)
        {
            _testUpdateVersion = null;
            RaiseStateChanged();
            return;
        }

        if (_updateManager is null || _pendingUpdate is null)
            return;

        _updateManager.ApplyUpdatesAndRestart(_pendingUpdate);
    }

    public void ShowTestUpdateNotification()
    {
        if (!IsDevelopmentEnvironment())
            return;

        if (_pendingUpdate is not null)
            return;

        _testUpdateVersion = "9.9.9-debug";
        RaiseStateChanged();
    }

    private static bool IsDevelopmentEnvironment() =>
        string.Equals(Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT"), "Development", StringComparison.OrdinalIgnoreCase);

    public void Dispose()
    {
        _lifetimeCts?.Cancel();
        _lifetimeCts?.Dispose();
        _checkGate.Dispose();
    }

    private async Task RunUpdateLoopAsync(CancellationToken cancellationToken)
    {
        await CheckForUpdatesAsync(cancellationToken).ConfigureAwait(false);

        var interval = NormalizeCheckInterval(_settings.CheckInterval);
        using var timer = new PeriodicTimer(interval, _timeProvider);

        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                await CheckForUpdatesAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown.
        }
    }

    private async Task CheckForUpdatesAsync(CancellationToken cancellationToken)
    {
        if (_updateManager is null || _pendingUpdate is not null)
            return;

        if (!await _checkGate.WaitAsync(0, cancellationToken).ConfigureAwait(false))
            return;

        try
        {
            var update = await _updateManager.CheckForUpdatesAsync().ConfigureAwait(false);
            if (update is null)
                return;

            await _updateManager.DownloadUpdatesAsync(update, cancelToken: cancellationToken).ConfigureAwait(false);
            RefreshPendingUpdateState();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Automatic update check failed: {ex}");
        }
        finally
        {
            _checkGate.Release();
        }
    }

    private void RefreshPendingUpdateState()
    {
        if (_updateManager is null)
            return;

        var pending = _updateManager.UpdatePendingRestart;
        var changed = !string.Equals(_pendingUpdate?.Version?.ToString(), pending?.Version?.ToString(), StringComparison.Ordinal);
        _pendingUpdate = pending;

        if (changed)
            RaiseStateChanged();
    }

    private void RaiseStateChanged()
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            StateChanged?.Invoke(this, EventArgs.Empty);
            return;
        }

        Dispatcher.UIThread.Post(() => StateChanged?.Invoke(this, EventArgs.Empty));
    }

    private static TimeSpan NormalizeCheckInterval(TimeSpan configuredInterval)
    {
        if (configuredInterval >= MinimumCheckInterval)
            return configuredInterval;

        if (configuredInterval <= TimeSpan.Zero)
            return DefaultCheckInterval;

        return MinimumCheckInterval;
    }
}
