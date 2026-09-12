using Avalonia.Media.Imaging;
using ConfigurableReader.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace ConfigurableReader.Models;

public class BookRecord : INotifyPropertyChanged
{
    private string _filePath = string.Empty;
    private string _title = string.Empty;
    private int _scrollPosition;
    private int _totalLength;
    private DateTime _lastReadDate;
    private string? _coverImagePath;
    private Bitmap? _coverBitmap;

    public string FilePath
    {
        get => _filePath;
        set => SetField(ref _filePath, value);
    }

    public string Title
    {
        get => _title;
        set => SetField(ref _title, value);
    }

    public int ScrollPosition
    {
        get => _scrollPosition;
        set
        {
            if (SetField(ref _scrollPosition, value))
            {
                OnPropertyChanged(nameof(ProgressPercentage));
                OnPropertyChanged(nameof(FormattedProgress));
            }
        }
    }

    public int TotalLength
    {
        get => _totalLength;
        set
        {
            if (SetField(ref _totalLength, value))
            {
                OnPropertyChanged(nameof(ProgressPercentage));
                OnPropertyChanged(nameof(FormattedProgress));
            }
        }
    }

    public DateTime LastReadDate
    {
        get => _lastReadDate;
        set => SetField(ref _lastReadDate, value);
    }

    public string? CoverImagePath
    {
        get => _coverImagePath;
        set => SetField(ref _coverImagePath, value);
    }

    [JsonIgnore]
    public Bitmap? CoverBitmap
    {
        get => _coverBitmap;
        set
        {
            if (SetField(ref _coverBitmap, value))
            {
                OnPropertyChanged(nameof(HasCoverBitmap));
            }
        }
    }

    [JsonIgnore]
    public bool HasCoverBitmap => CoverBitmap != null;

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

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
