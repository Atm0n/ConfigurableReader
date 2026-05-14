using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ConfigurableReader.Core;
using VersOne.Epub;

namespace ConfigurableReader.Parsers.Epub;

public class EpubBookParser : IBookParser
{
    public string FormatName => "EPUB Books";
    public string[] SupportedExtensions => new[] { ".epub" };

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

        // Basic HTML tag stripping
        string step1 = Regex.Replace(html, "<[^>]*>", " ");
        
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
        
        // Collapse multiple spaces into one
        return Regex.Replace(step1, @"\s+", " ").Trim();
    }
}
