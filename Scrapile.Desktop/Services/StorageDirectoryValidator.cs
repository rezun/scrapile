namespace Scrapile.Desktop.Services;

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Result of validating a storage directory.
/// </summary>
public enum StorageDirectoryValidationResult
{
    /// <summary>
    /// Directory contains .ephemeral_metadata.json (existing Scrapile folder).
    /// </summary>
    ExistingScrapileFolder,

    /// <summary>
    /// Directory is empty or doesn't exist.
    /// </summary>
    EmptyFolder,

    /// <summary>
    /// Directory contains files but no metadata (invalid for Scrapile).
    /// </summary>
    InvalidFolder
}

/// <summary>
/// Service for validating and managing storage directory changes.
/// </summary>
public class StorageDirectoryValidator
{
    private const string MetadataFileName = ".ephemeral_metadata.json";

    /// <summary>
    /// Validates whether a directory is suitable for use as a Scrapile storage directory.
    /// </summary>
    /// <param name="path">The path to validate.</param>
    /// <returns>The validation result.</returns>
    public StorageDirectoryValidationResult ValidateDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return StorageDirectoryValidationResult.EmptyFolder;
        }

        if (!Directory.Exists(path))
        {
            return StorageDirectoryValidationResult.EmptyFolder;
        }

        var metadataPath = Path.Combine(path, MetadataFileName);
        if (File.Exists(metadataPath))
        {
            return StorageDirectoryValidationResult.ExistingScrapileFolder;
        }

        // Check if directory has any files
        var hasFiles = Directory.EnumerateFileSystemEntries(path).Any();
        if (!hasFiles)
        {
            return StorageDirectoryValidationResult.EmptyFolder;
        }

        return StorageDirectoryValidationResult.InvalidFolder;
    }

    /// <summary>
    /// Checks if the source directory has any data to copy.
    /// </summary>
    /// <param name="sourceDirectory">The source directory to check.</param>
    /// <returns>True if there is data to copy.</returns>
    public bool HasDataToCopy(string? sourceDirectory)
    {
        if (string.IsNullOrWhiteSpace(sourceDirectory) || !Directory.Exists(sourceDirectory))
        {
            return false;
        }

        var metadataPath = Path.Combine(sourceDirectory, MetadataFileName);
        if (File.Exists(metadataPath))
        {
            return true;
        }

        return Directory.EnumerateFiles(sourceDirectory, "*.txt").Any();
    }

    /// <summary>
    /// Copies Scrapile data from source to target directory.
    /// </summary>
    /// <param name="source">The source directory.</param>
    /// <param name="target">The target directory.</param>
    /// <exception cref="IOException">If copy fails.</exception>
    /// <exception cref="UnauthorizedAccessException">If access is denied.</exception>
    public async Task CopyDataAsync(string source, string target)
    {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target))
        {
            throw new ArgumentException("Source and target directories must be specified.");
        }

        if (!Directory.Exists(source))
        {
            throw new DirectoryNotFoundException($"Source directory not found: {source}");
        }

        // Ensure target directory exists
        Directory.CreateDirectory(target);

        // Copy metadata file if it exists
        var sourceMetadata = Path.Combine(source, MetadataFileName);
        if (File.Exists(sourceMetadata))
        {
            var targetMetadata = Path.Combine(target, MetadataFileName);
            await CopyFileAsync(sourceMetadata, targetMetadata);
        }

        // Copy all .txt files
        foreach (var sourceFile in Directory.EnumerateFiles(source, "*.txt"))
        {
            var fileName = Path.GetFileName(sourceFile);
            var targetFile = Path.Combine(target, fileName);
            await CopyFileAsync(sourceFile, targetFile);
        }
    }

    private static async Task CopyFileAsync(string source, string target)
    {
        const int bufferSize = 81920; // 80KB buffer
        await using var sourceStream = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
        await using var targetStream = new FileStream(target, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
        await sourceStream.CopyToAsync(targetStream);
    }
}
