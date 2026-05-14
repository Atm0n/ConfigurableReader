using System.Text;
using System.Text.RegularExpressions;
using ConfigurableReader.Core;
using VersOne.Epub;
using HtmlAgilityPack;

namespace ConfigurableReader.Parsers.Epub;

public partial class EpubBookParser : IBookParser
{
    public string FormatName => "EPUB Books";
    public string[] SupportedExtensions => [".epub"];

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    public async Task<string> ExtractTextAsync(string filePath)
    {
        EpubBook book = await EpubReader.ReadBookAsync(filePath);
        StringBuilder sb = new();

        foreach (var textContentFile in book.ReadingOrder)
        {
            string plainText = ExtractTextFromHtml(textContentFile.Content);
            if (!string.IsNullOrWhiteSpace(plainText))
            {
                sb.Append(plainText);
                sb.Append(' ');
            }
        }

        return NormalizeWhitespace(sb.ToString());
    }

    private static string ExtractTextFromHtml(string html)
    {
        if (string.IsNullOrEmpty(html)) return string.Empty;

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Remove style, script, head, and meta tags
        var nodesToRemove = doc.DocumentNode.SelectNodes("//style|//script|//head|//meta|//link");
        if (nodesToRemove != null)
        {
            foreach (var node in nodesToRemove)
            {
                node.Remove();
            }
        }

        // Add spaces between elements that are usually block-level to prevent words merging
        var blockNodes = doc.DocumentNode.SelectNodes("//p|//div|//h1|//h2|//h3|//h4|//h5|//h6|//li|//br");
        if (blockNodes != null)
        {
            foreach (var node in blockNodes)
            {
                node.ParentNode.InsertAfter(doc.CreateTextNode(" "), node);
            }
        }

        return doc.DocumentNode.InnerText;
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
