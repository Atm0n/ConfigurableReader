using System.Text;
using System.Text.RegularExpressions;
using ConfigurableReader.Core;
using VersOne.Epub;

namespace ConfigurableReader.Parsers.Epub;

public partial class EpubBookParser : IBookParser
{
    public string FormatName => "EPUB Books";
    public string[] SupportedExtensions => new[] { ".epub" };

    [GeneratedRegex("<[^>]*>")]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    public async Task<string> ExtractTextAsync(string filePath)
    {
        EpubBook book = await EpubReader.ReadBookAsync(filePath);
        StringBuilder sb = new();

        foreach (var textContentFile in book.ReadingOrder)
        {
            string plainText = StripHtml(textContentFile.Content);
            sb.Append(plainText);
            sb.Append(" ");
        }

        return NormalizeWhitespace(sb.ToString());
    }

    private string StripHtml(string html)
    {
        if (string.IsNullOrEmpty(html)) return string.Empty;

        // Use source-generated regex for HTML tag stripping
        string step1 = HtmlTagRegex().Replace(html, " ");

        // Decode common entities (very basic)
        string step2 = step1.Replace("&nbsp;", " ")
                            .Replace("&lt;", "<")
                            .Replace("&gt;", ">")
                            .Replace("&amp;", "&")
                            .Replace("&quot;", "\"")
                            .Replace("&apos;", "'");

        return step2;
    }

    private string NormalizeWhitespace(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        // Replace newlines and tabs with spaces
        string step1 = text.Replace("\r", " ").Replace("\n", " ").Replace("\t", " ");

        // Use source-generated regex to collapse multiple spaces into one
        return WhitespaceRegex().Replace(step1, " ").Trim();
    }
}
