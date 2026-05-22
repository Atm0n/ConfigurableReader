using System.Collections.Generic;
using System.Collections.ObjectModel;
using ConfigurableReader.Core;

namespace ConfigurableReader.Models;

public class BookRecord
{
    public string FilePath { get; set; } = string.Empty;
    public int ScrollPosition { get; set; }
    public ObservableCollection<BookmarkItem> CustomBookmarks { get; set; } = new ObservableCollection<BookmarkItem>();
}
