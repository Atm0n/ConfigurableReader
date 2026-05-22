using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ConfigurableReader.Core;
using VersOne.Epub;

namespace ConfigurableReader.Parsers.Epub;

public class EpubBookSource : IBookSource
{
    private readonly EpubBook _book;
    private readonly List<EpubChapterInfo> _chapters = new();
    private readonly int _totalLength;

    // Thread-safe LRU Cache
    private readonly Dictionary<int, string> _chapterTextCache = new();
    private readonly List<int> _cacheOrder = new();
    private const int MaxCacheSize = 5;
    private readonly object _cacheLock = new();

    public int TotalLength => _totalLength;
    public IReadOnlyList<BookmarkItem> TableOfContents { get; }

    public EpubBookSource(EpubBook book)
    {
        _book = book ?? throw new ArgumentNullException(nameof(book));

        int currentStart = 0;
        for (int i = 0; i < _book.ReadingOrder.Count; i++)
        {
            var contentFile = _book.ReadingOrder[i];
            string rawHtml = contentFile.Content ?? string.Empty;
            
            // Build the chapter plain-text and capture its normalized character length
            string plainText = EpubBookParser.NormalizeWhitespace(EpubBookParser.ExtractTextFromHtml(rawHtml));
            int length = plainText.Length;

            _chapters.Add(new EpubChapterInfo(i, currentStart, length));
            currentStart += length + 1; // 1 virtual space between chapters
        }

        _totalLength = Math.Max(0, currentStart - 1);

        TableOfContents = BuildToc(_book.Navigation);
    }

    private List<BookmarkItem> BuildToc(List<EpubNavigationItem>? navigationItems)
    {
        var toc = new List<BookmarkItem>();
        if (navigationItems == null) return toc;

        foreach (var item in navigationItems)
        {
            var bookmark = new BookmarkItem { Title = item.Title ?? "Untitled" };

            // Find matching chapter based on the file link
            if (!string.IsNullOrEmpty(item.Link?.ContentFilePath))
            {
                var fileIndex = _book.ReadingOrder.FindIndex(c => c.FilePath == item.Link.ContentFilePath);
                if (fileIndex >= 0 && fileIndex < _chapters.Count)
                {
                    bookmark.Position = _chapters[fileIndex].StartPosition;
                }
            }

            bookmark.SubItems = BuildToc(item.NestedItems);
            toc.Add(bookmark);
        }
        return toc;
    }

    public async Task<string> GetTextAsync(int start, int count)
    {
        if (start < 0) start = 0;
        if (start >= _totalLength || count <= 0) return string.Empty;

        int end = Math.Min(start + count, _totalLength);
        StringBuilder sb = new();

        for (int i = 0; i < _chapters.Count; i++)
        {
            var chapter = _chapters[i];
            int chapterStart = chapter.StartPosition;
            int chapterEnd = chapter.EndPosition;

            // 1. Slice chapter content if it falls inside the range
            if (start < chapterEnd && end > chapterStart)
            {
                int sliceStart = Math.Max(start, chapterStart);
                int sliceEnd = Math.Min(end, chapterEnd);
                int relativeStart = sliceStart - chapterStart;
                int relativeCount = sliceEnd - sliceStart;

                string chapterText = await GetChapterTextAsync(chapter.Index);
                
                if (relativeStart < chapterText.Length)
                {
                    int actualCount = Math.Min(relativeCount, chapterText.Length - relativeStart);
                    if (actualCount > 0)
                    {
                        sb.Append(chapterText.Substring(relativeStart, actualCount));
                    }
                }
            }

            // 2. Include the virtual single-space divider between chapters if spanned
            if (i < _chapters.Count - 1)
            {
                int spacePos = chapter.EndPosition;
                if (start <= spacePos && end > spacePos)
                {
                    sb.Append(' ');
                }
            }
        }

        return sb.ToString();
    }

    private async Task<string> GetChapterTextAsync(int index)
    {
        lock (_cacheLock)
        {
            if (_chapterTextCache.TryGetValue(index, out string? cachedText))
            {
                _cacheOrder.Remove(index);
                _cacheOrder.Add(index);
                return cachedText;
            }
        }

        // Parse asynchronously/off-thread to keep UI interactions non-blocking
        var contentFile = _book.ReadingOrder[index];
        string rawHtml = await Task.Run(() => contentFile.Content ?? string.Empty);
        string plainText = EpubBookParser.NormalizeWhitespace(EpubBookParser.ExtractTextFromHtml(rawHtml));

        lock (_cacheLock)
        {
            if (_chapterTextCache.TryGetValue(index, out string? cachedText))
            {
                return cachedText;
            }

            if (_chapterTextCache.Count >= MaxCacheSize)
            {
                int oldestIndex = _cacheOrder[0];
                _cacheOrder.RemoveAt(0);
                _chapterTextCache.Remove(oldestIndex);
            }

            _chapterTextCache[index] = plainText;
            _cacheOrder.Add(index);
            return plainText;
        }
    }

    public void Dispose()
    {
        lock (_cacheLock)
        {
            _chapterTextCache.Clear();
            _cacheOrder.Clear();
        }
    }
}

internal record EpubChapterInfo(int Index, int StartPosition, int Length)
{
    public int EndPosition => StartPosition + Length;
}
