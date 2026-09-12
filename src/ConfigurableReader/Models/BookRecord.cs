using ConfigurableReader.Core;
using System;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace ConfigurableReader.Models;

public class BookRecord
{
    public string FilePath { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int ScrollPosition { get; set; }
    public int TotalLength { get; set; }
    public DateTime LastReadDate { get; set; }

    public ObservableCollection<BookmarkItem> CustomBookmarks { get; set; } = new ObservableCollection<BookmarkItem>();

    [JsonIgnore]
    public double ProgressPercentage => TotalLength > 0 ? (double)ScrollPosition / TotalLength * 100 : 0;

    [JsonIgnore]
    public string FormattedProgress => TotalLength > 0 ? $"{Math.Clamp(ProgressPercentage, 0, 100):F1}%" : "0%";

    [JsonIgnore]
    public bool IsWebArticle => FilePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                                FilePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

    [JsonIgnore]
    public string CoverIcon => IsWebArticle ? "🌐" : "📖";

    [JsonIgnore]
    public string DisplayTitle => string.IsNullOrEmpty(Title) ? System.IO.Path.GetFileNameWithoutExtension(FilePath) : Title;
}
