using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ConfigurableReader.Models;

/// <summary>
/// Persists per-book reading positions to a dedicated file, separate from app settings.
/// </summary>
public static class BookRecordStore
{
    private static readonly string StorePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ConfigurableReader",
        "bookrecords.json");

    public static List<BookRecord> Load()
    {
        try
        {
            if (File.Exists(StorePath))
            {
                string json = File.ReadAllText(StorePath);
                return JsonSerializer.Deserialize<List<BookRecord>>(json) ?? [];
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load book records: {ex.Message}");
        }
        return [];
    }

    public static void Save(List<BookRecord> records)
    {
        try
        {
            string? directory = Path.GetDirectoryName(StorePath);
            if (directory != null) Directory.CreateDirectory(directory);
            string json = JsonSerializer.Serialize(records, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(StorePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save book records: {ex.Message}");
        }
    }
}
