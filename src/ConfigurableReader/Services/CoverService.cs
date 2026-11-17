using Avalonia.Media.Imaging;
using ConfigurableReader.Common;
using ConfigurableReader.Core;
using ConfigurableReader.Models;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConfigurableReader.Services;

public class CoverService
{
    private static readonly string DefaultCoversDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ConfigurableReader",
        "Covers");

    /// <summary>
    /// How long a "this book has no cover" result is trusted before the parser is asked again.
    /// Keeps library loads cheap (no re-parsing / re-downloading) while still recovering
    /// from transient failures such as a network error on a web article.
    /// </summary>
    public static readonly TimeSpan NoCoverRetryInterval = TimeSpan.FromDays(7);

    private readonly DocumentRegistry _documentRegistry;
    private readonly string _coversDirectory;
    private readonly ConcurrentDictionary<string, Bitmap?> _bitmapCache = new();

    public CoverService(DocumentRegistry documentRegistry, string? coversDirectory = null)
    {
        _documentRegistry = documentRegistry;
        _coversDirectory = coversDirectory ?? DefaultCoversDirectory;
        try
        {
            Directory.CreateDirectory(_coversDirectory);
        }
        catch (Exception ex)
        {
            AppLog.Error("Failed to create covers directory", ex);
        }
    }

    private static string GetCacheKey(string identifier)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(identifier.ToLowerInvariant()));
        return Convert.ToHexString(hash)[..16].ToLowerInvariant();
    }

    public static string GetCoverCachePath(string identifier) =>
        Path.Combine(DefaultCoversDirectory, GetCacheKey(identifier) + ".png");

    private string GetCoverPath(string identifier) =>
        Path.Combine(_coversDirectory, GetCacheKey(identifier) + ".png");

    private string GetNoCoverMarkerPath(string identifier) =>
        Path.Combine(_coversDirectory, GetCacheKey(identifier) + ".nocover");

    public async Task<string?> EnsureCoverExtractedAsync(string filePathOrUrl, CancellationToken cancellationToken = default)
    {
        string cachePath = GetCoverPath(filePathOrUrl);
        if (File.Exists(cachePath) && new FileInfo(cachePath).Length > 0)
        {
            return cachePath;
        }

        string markerPath = GetNoCoverMarkerPath(filePathOrUrl);
        if (File.Exists(markerPath) &&
            DateTime.UtcNow - File.GetLastWriteTimeUtc(markerPath) < NoCoverRetryInterval)
        {
            return null;
        }

        var parser = _documentRegistry.GetParserForFile(filePathOrUrl);
        if (parser == null) return null;

        try
        {
            // Parsers may do synchronous work before their first await; keep it off the caller's thread.
            byte[]? imageBytes = await Task.Run(() => parser.ExtractCoverImageAsync(filePathOrUrl), cancellationToken);
            if (imageBytes != null && imageBytes.Length > 0)
            {
                await File.WriteAllBytesAsync(cachePath, imageBytes, cancellationToken);
                TryDelete(markerPath);
                return cachePath;
            }

            await File.WriteAllBytesAsync(markerPath, [], cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            AppLog.Error($"Failed to extract cover for {filePathOrUrl}", ex);
        }

        return null;
    }

    public Bitmap? GetCoverBitmap(string? coverPath)
    {
        if (string.IsNullOrEmpty(coverPath) || !File.Exists(coverPath)) return null;

        return _bitmapCache.GetOrAdd(coverPath, path =>
        {
            try
            {
                return new Bitmap(path);
            }
            catch (Exception ex)
            {
                AppLog.Error($"Failed to load bitmap from {path}", ex);
                return null;
            }
        });
    }

    /// <summary>
    /// Extracts and decodes the cover on a background thread, then assigns it to the record
    /// on the calling context. Call from the UI thread so bound properties change on the UI thread.
    /// </summary>
    public async Task PopulateCoverForRecordAsync(BookRecord record, CancellationToken cancellationToken = default)
    {
        string filePath = record.FilePath;
        string? knownPath = record.CoverImagePath;

        try
        {
            var (coverPath, bitmap) = await Task.Run(async () =>
            {
                string? path = !string.IsNullOrEmpty(knownPath) && File.Exists(knownPath)
                    ? knownPath
                    : await EnsureCoverExtractedAsync(filePath, cancellationToken);
                return (path, path != null ? GetCoverBitmap(path) : null);
            }, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            if (coverPath != null)
            {
                record.CoverImagePath = coverPath;
            }
            if (bitmap != null)
            {
                record.CoverBitmap = bitmap;
            }
        }
        catch (OperationCanceledException)
        {
            // A newer library refresh superseded this one.
        }
        catch (Exception ex)
        {
            AppLog.Error($"Failed to populate cover for {filePath}", ex);
        }
    }

    private static void TryDelete(string path)
    {
        try { File.Delete(path); } catch { }
    }
}
