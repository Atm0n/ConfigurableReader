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
        string text = await ExtractTextAsync(filePath);
        return new MemoryBookSource(text);
    }

    public async Task<string> ExtractTextAsync(string filePath)
    {
        return await Task.Run(() =>
        {
            StringBuilder sb = new();
            using (PdfDocument document = PdfDocument.Open(filePath))
            {
                foreach (var page in document.GetPages())
                {
                    // Extract text from the page
                    string pageText = page.Text;
                    if (!string.IsNullOrWhiteSpace(pageText))
                    {
                        sb.Append(pageText);
                        sb.Append(' ');
                    }
                }
            }

            return NormalizeWhitespace(sb.ToString());
        });
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
