namespace Scrapile.Domain.Interfaces;

/// <summary>
/// Information about an existing lock on a storage directory.
/// </summary>
/// <param name="Pid">Process ID of the instance holding the lock.</param>
/// <param name="StartedAt">When the locking instance started.</param>
/// <param name="MachineName">Name of the machine where the instance is running.</param>
public record LockInfo(int Pid, DateTime StartedAt, string? MachineName);

/// <summary>
/// Service for managing exclusive locks on storage directories.
/// Prevents multiple instances from using the same storage folder.
/// </summary>
public interface IStorageLockService : IDisposable
{
    /// <summary>
    /// Attempts to acquire an exclusive lock on the storage directory.
    /// </summary>
    /// <param name="storageDirectory">The directory to lock.</param>
    /// <returns>True if lock acquired, false if another instance holds the lock.</returns>
    bool TryAcquireLock(string storageDirectory);

    /// <summary>
    /// Releases the lock if held by this instance.
    /// </summary>
    void ReleaseLock();

    /// <summary>
    /// Gets info about the process holding the lock (for error messages).
    /// </summary>
    /// <param name="storageDirectory">The directory to check.</param>
    /// <returns>Lock info if a lock file exists, null otherwise.</returns>
    LockInfo? GetExistingLockInfo(string storageDirectory);
}
