using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ConfigurableReader.Core;

namespace ConfigurableReader.Parsers.Txt;

public class TxtBookSource : IBookSource
{
    private readonly string _text;

    public int TotalLength => _text.Length;
    public IReadOnlyList<BookmarkItem> TableOfContents { get; } = [];

    public TxtBookSource(string filePath)
    {
        string rawText = File.ReadAllText(filePath);
        _text = NormalizeWhitespace(rawText);
    }

    public static async Task<TxtBookSource> CreateAsync(string filePath)
    {
        string rawText = await File.ReadAllTextAsync(filePath);
        return new TxtBookSource(NormalizeWhitespace(rawText), isDirectText: true);
    }

    private TxtBookSource(string normalizedText, bool isDirectText)
    {
        _text = normalizedText;
    }

    private static string NormalizeWhitespace(string raw)
    {
        return raw.Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ');
    }

    public Task<string> GetTextAsync(int start, int count)
    {
        if (start < 0) start = 0;
        if (start >= _text.Length || count <= 0) return Task.FromResult(string.Empty);

        int actualCount = Math.Min(count, _text.Length - start);
        return Task.FromResult(_text.Substring(start, actualCount));
    }

    public void Dispose()
    {
    }
}
