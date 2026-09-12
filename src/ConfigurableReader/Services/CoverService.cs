using Avalonia.Media.Imaging;
using ConfigurableReader.Core;
using ConfigurableReader.Models;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ConfigurableReader.Services;

public class CoverService
{
    private static readonly string CoversDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ConfigurableReader",
        "Covers");

    private readonly DocumentRegistry _documentRegistry;
    private readonly ConcurrentDictionary<string, Bitmap?> _bitmapCache = new();

    public CoverService(DocumentRegistry documentRegistry)
    {
        _documentRegistry = documentRegistry;
        try
        {
            Directory.CreateDirectory(CoversDirectory);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to create covers directory: {ex.Message}");
        }
    }

    public static string GetCoverCachePath(string identifier)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(identifier.ToLowerInvariant()));
        string hex = Convert.ToHexString(hash)[..16].ToLowerInvariant();
        return Path.Combine(CoversDirectory, hex + ".png");
    }

    public async Task<string?> EnsureCoverExtractedAsync(string filePathOrUrl)
    {
        string cachePath = GetCoverCachePath(filePathOrUrl);
        if (File.Exists(cachePath) && new FileInfo(cachePath).Length > 0)
        {
            return cachePath;
        }

        var parser = _documentRegistry.GetParserForFile(filePathOrUrl);
        if (parser == null) return null;

        try
        {
            byte[]? imageBytes = await parser.ExtractCoverImageAsync(filePathOrUrl);
            if (imageBytes != null && imageBytes.Length > 0)
            {
                await File.WriteAllBytesAsync(cachePath, imageBytes);
                return cachePath;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to extract cover for {filePathOrUrl}: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"Failed to load bitmap from {path}: {ex.Message}");
                return null;
            }
        });
    }

    public async Task PopulateCoverForRecordAsync(BookRecord record)
    {
        if (string.IsNullOrEmpty(record.CoverImagePath) || !File.Exists(record.CoverImagePath))
        {
            string? extractedPath = await EnsureCoverExtractedAsync(record.FilePath);
            if (extractedPath != null)
            {
                record.CoverImagePath = extractedPath;
            }
        }

        if (!string.IsNullOrEmpty(record.CoverImagePath))
        {
            record.CoverBitmap = GetCoverBitmap(record.CoverImagePath);
        }
    }
}
