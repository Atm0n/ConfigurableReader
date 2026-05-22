using System.Text;
using ConfigurableReader.Core;

namespace ConfigurableReader.Parsers.Txt;

public class TxtBookSource : IBookSource
{
    private readonly string _filePath;
    private readonly FileStream _fileStream;
    private readonly int _totalLength;
    private readonly Encoding _encoding;

    public int TotalLength => _totalLength;
    public IReadOnlyList<BookmarkItem> TableOfContents { get; } = new List<BookmarkItem>();

    public TxtBookSource(string filePath)
    {
        _filePath = filePath;
        _fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        
        // For simplicity in this first pass, we assume 1 byte = 1 char (ASCII/UTF8 without multibyte)
        // A more robust implementation would handle encoding properly for seeking.
        _totalLength = (int)_fileStream.Length;
        _encoding = Encoding.UTF8;
    }

    public async Task<string> GetTextAsync(int start, int count)
    {
        if (start < 0) start = 0;
        if (start >= _totalLength) return string.Empty;
        
        int actualCount = Math.Min(count, _totalLength - start);
        byte[] buffer = new byte[actualCount];
        
        _fileStream.Seek(start, SeekOrigin.Begin);
        int bytesRead = 0;
        while (bytesRead < actualCount)
        {
            int n = await _fileStream.ReadAsync(buffer, bytesRead, actualCount - bytesRead);
            if (n == 0) break;
            bytesRead += n;
        }
        
        // Clean up basic whitespace/control chars similar to previous implementation
        string text = _encoding.GetString(buffer);
        return text.Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ');
    }

    public void Dispose()
    {
        _fileStream.Dispose();
    }
}
