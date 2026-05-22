using System.Text;
using System.Text.RegularExpressions;
using ConfigurableReader.Core;
using UglyToad.PdfPig;

namespace ConfigurableReader.Parsers.Pdf;

public partial class PdfBookParser : IBookParser
{
    public string FormatName => "PDF Documents";
    public string[] SupportedExtensions => new[] { ".pdf" };

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    public async Task<IBookSource> CreateSourceAsync(string filePath)
    {
        var (text, toc) = await ExtractTextAsync(filePath);
        return new MemoryBookSource(text, toc);
    }

    private async Task<(string Text, List<BookmarkItem> Toc)> ExtractTextAsync(string filePath)
    {
        return await Task.Run(() =>
        {
            StringBuilder sb = new();
            Dictionary<int, int> pageOffsets = new();
            List<BookmarkItem> toc = new();

            using (PdfDocument document = PdfDocument.Open(filePath))
            {
                foreach (var page in document.GetPages())
                {
                    pageOffsets[page.Number] = sb.Length;
                    string pageText = page.Text;
                    if (!string.IsNullOrWhiteSpace(pageText))
                    {
                        sb.Append(NormalizeWhitespace(pageText));
                        sb.Append(' ');
                    }
                }

                if (document.TryGetBookmarks(out var bookmarks))
                {
                    toc = BuildToc(bookmarks.GetNodes(), pageOffsets);
                }
            }

            // Trim end just in case
            return (sb.ToString().TrimEnd(), toc);
        });
    }

    private static List<BookmarkItem> BuildToc(IEnumerable<UglyToad.PdfPig.Outline.BookmarkNode> nodes, Dictionary<int, int> pageOffsets)
    {
        var result = new List<BookmarkItem>();
        foreach (var node in nodes)
        {
            var item = new BookmarkItem { Title = node.Title };
            if (node is UglyToad.PdfPig.Outline.DocumentBookmarkNode docNode && 
                docNode.Destination != null && 
                pageOffsets.TryGetValue(docNode.Destination.PageNumber, out int pos))
            {
                item.Position = pos;
            }
            if (node.Children != null && node.Children.Count > 0)
            {
                item.SubItems = BuildToc(node.Children, pageOffsets);
            }
            result.Add(item);
        }
        return result;
    }

    private static string NormalizeWhitespace(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        // Replace newlines, tabs, etc. with spaces
        string step1 = text.Replace("\r", " ")
                           .Replace("\n", " ")
                           .Replace("\t", " ");

        // Collapse multiple spaces
        return WhitespaceRegex().Replace(step1, " ").Trim();
    }
}
