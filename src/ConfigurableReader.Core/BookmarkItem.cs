namespace ConfigurableReader.Core;

public class BookmarkItem
{
    public string Title { get; set; } = string.Empty;
    public int Position { get; set; }
    public List<BookmarkItem> SubItems { get; set; } = new();

    public BookmarkItem() { }

    public BookmarkItem(string title, int position)
    {
        Title = title;
        Position = position;
    }
}
