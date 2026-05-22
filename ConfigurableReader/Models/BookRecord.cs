using System.Collections.Generic;
using ConfigurableReader.Core;

namespace ConfigurableReader.Models;

public class BookRecord
{
    public string FilePath { get; set; } = string.Empty;
    public int ScrollPosition { get; set; }
    public List<BookmarkItem> CustomBookmarks { get; set; } = new();
}
