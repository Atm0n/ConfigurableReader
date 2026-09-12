namespace ConfigurableReader.Core;

/// <summary>
/// A simple memory-backed source for smaller files or parsers that don't support streaming yet.
/// </summary>
public class MemoryBookSource : IBookSource
{
    private readonly string _fullText;
    public int TotalLength => _fullText.Length;
    public IReadOnlyList<BookmarkItem> TableOfContents { get; }
    public string? Title { get; }

    public MemoryBookSource(string fullText, IReadOnlyList<BookmarkItem>? tableOfContents = null, string? title = null)
    {
        _fullText = fullText ?? string.Empty;
        TableOfContents = tableOfContents ?? new List<BookmarkItem>();
        Title = title;
    }

    public Task<string> GetTextAsync(int start, int count)
    {
        if (start < 0) start = 0;
        if (start >= _fullText.Length) return Task.FromResult(string.Empty);

        int actualCount = Math.Min(count, _fullText.Length - start);
        return Task.FromResult(_fullText.Substring(start, actualCount));
    }

    public void Dispose() { }
}
