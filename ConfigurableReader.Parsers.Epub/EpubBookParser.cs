using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ConfigurableReader.Core;
using VersOne.Epub;

namespace ConfigurableReader.Parsers.Epub;

public partial class EpubBookParser : IBookParser
{
    public string FormatName => "EPUB Books";
    public string[] SupportedExtensions => [".epub"];

    [GeneratedRegex("<[^>]*>")]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"<(style|script|head)[^>]*>.*?</\1>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex ContentBlockRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    public async Task<string> ExtractTextAsync(string filePath)
    {
        EpubBook book = await EpubReader.ReadBookAsync(filePath);
        StringBuilder sb = new();

        foreach (var textContentFile in book.ReadingOrder)
        {
            string plainText = StripHtml(textContentFile.Content);
            if (!string.IsNullOrWhiteSpace(plainText))
            {
                sb.Append(plainText);
                sb.Append(' ');
            }
        }

        return NormalizeWhitespace(sb.ToString());
    }

    private static string StripHtml(string html)
    {
        if (string.IsNullOrEmpty(html)) return string.Empty;

        // 1. Remove blocks that shouldn't be treated as text (style, script, head)
        string step1 = ContentBlockRegex().Replace(html, " ");

        // 2. Strip all remaining HTML tags
        string step2 = HtmlTagRegex().Replace(step1, " ");

        // 3. Decode HTML entities properly using WebUtility
        return WebUtility.HtmlDecode(step2);
    }

    private static string NormalizeWhitespace(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        // Replace newlines, tabs, and non-breaking spaces with standard spaces
        string step1 = text.Replace("\r", " ")
                           .Replace("\n", " ")
                           .Replace("\t", " ")
                           .Replace("\u00A0", " ");

        // Use source-generated regex to collapse multiple spaces into one
        return WhitespaceRegex().Replace(step1, " ").Trim();
    }
}
