using System.Collections.Generic;

namespace ConfigurableReader.Core;

public class BookmarkItem
{
    public string Title { get; set; } = string.Empty;
    public int Position { get; set; }
    public List<BookmarkItem> SubItems { get; set; } = new();
}
