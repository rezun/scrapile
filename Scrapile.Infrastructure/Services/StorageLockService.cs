namespace Scrapile.Infrastructure.Services;

using System.Diagnostics;
using System.Text.Json;
using Scrapile.Domain.Interfaces;

/// <summary>
/// JSON structure for the lock file content.
/// </summary>
internal class LockFileContent
{
    public int Pid { get; set; }
    public DateTime StartedAt { get; set; }
    public string? MachineName { get; set; }
}

/// <summary>
/// File-based implementation of IStorageLockService.
/// Uses a lock file with an exclusive file handle to prevent multiple instances.
/// </summary>
public class StorageLockService : IStorageLockService
{
    private const string LockFilename = ".scrapile.lock";

    private FileStream? _lockFileHandle;
    private string? _lockFilePath;
    private bool _disposed;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    /// <inheritdoc />
    public bool TryAcquireLock(string storageDirectory)
    {
        if (string.IsNullOrWhiteSpace(storageDirectory))
        {
            throw new ArgumentException("Storage directory cannot be null or empty.", nameof(storageDirectory));
        }

        // Ensure directory exists
        Directory.CreateDirectory(storageDirectory);

        var lockPath = Path.Combine(storageDirectory, LockFilename);

        // Check for existing lock file
        if (File.Exists(lockPath))
        {
            var existingLock = ReadLockFile(lockPath);
            if (existingLock != null && IsProcessRunning(existingLock.Pid))
            {
                // Another instance is actively running
                return false;
            }

            // Stale lock from crash - remove it
            try
            {
                File.Delete(lockPath);
            }
            catch (IOException)
            {
                // File is locked by another process that's actually running
                return false;
            }
        }

        // Try to acquire exclusive lock
        try
        {
            // Open with FileShare.Read so other instances can read lock info for error messages
            _lockFileHandle = new FileStream(
                lockPath,
                FileMode.Create,
                FileAccess.ReadWrite,
                FileShare.Read,
                bufferSize: 4096,
                FileOptions.DeleteOnClose);

            _lockFilePath = lockPath;

            // Write lock info to file
            var lockContent = new LockFileContent
            {
                Pid = Environment.ProcessId,
                StartedAt = DateTime.UtcNow,
                MachineName = Environment.MachineName
            };

            var json = JsonSerializer.Serialize(lockContent, JsonOptions);
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            _lockFileHandle.Write(bytes);
            _lockFileHandle.Flush();

            return true;
        }
        catch (IOException)
        {
            // Another process beat us to it
            _lockFileHandle?.Dispose();
            _lockFileHandle = null;
            _lockFilePath = null;
            return false;
        }
    }

    /// <inheritdoc />
    public void ReleaseLock()
    {
        if (_lockFileHandle != null)
        {
            _lockFileHandle.Dispose();
            _lockFileHandle = null;
        }

        // FileOptions.DeleteOnClose handles file deletion
        _lockFilePath = null;
    }

    /// <inheritdoc />
    public LockInfo? GetExistingLockInfo(string storageDirectory)
    {
        if (string.IsNullOrWhiteSpace(storageDirectory))
        {
            return null;
        }

        var lockPath = Path.Combine(storageDirectory, LockFilename);
        var content = ReadLockFile(lockPath);

        if (content == null)
        {
            return null;
        }

        return new LockInfo(content.Pid, content.StartedAt, content.MachineName);
    }

    /// <summary>
    /// Reads and parses an existing lock file.
    /// </summary>
    private static LockFileContent? ReadLockFile(string lockPath)
    {
        if (!File.Exists(lockPath))
        {
            return null;
        }

        try
        {
            // Try to read the file - if it's locked by another instance, this may fail
            var json = File.ReadAllText(lockPath);
            return JsonSerializer.Deserialize<LockFileContent>(json, JsonOptions);
        }
        catch (IOException)
        {
            // File is locked or inaccessible - assume another instance has it
            return new LockFileContent
            {
                Pid = 0,
                StartedAt = DateTime.UtcNow,
                MachineName = "Unknown (file locked)"
            };
        }
        catch (JsonException)
        {
            // Corrupted lock file
            return null;
        }
    }

    /// <summary>
    /// Checks if a process with the given PID is still running and is a Scrapile instance.
    /// Also verifies the process name to prevent PID reuse issues on Unix systems.
    /// </summary>
    private static bool IsProcessRunning(int pid)
    {
        if (pid <= 0)
        {
            return true; // Assume running if we couldn't read PID
        }

        try
        {
            var process = Process.GetProcessById(pid);
            // Process exists - check if it hasn't exited
            if (process.HasExited)
            {
                return false;
            }

            // Verify it's actually Scrapile to prevent PID reuse issues
            // On Unix systems, PIDs can be reused quickly after a process exits
            try
            {
                var processName = process.ProcessName;
                return processName.Contains("Scrapile", StringComparison.OrdinalIgnoreCase) ||
                       processName.Contains("dotnet", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                // Can't read process name (permissions, platform limitations)
                // Fall back to assuming it's running to be safe
                return true;
            }
        }
        catch (ArgumentException)
        {
            // Process not found
            return false;
        }
        catch (InvalidOperationException)
        {
            // Process has exited
            return false;
        }
    }

    /// <summary>
    /// Disposes the lock service and releases any held lock.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            ReleaseLock();
            _disposed = true;
        }
    }
}
