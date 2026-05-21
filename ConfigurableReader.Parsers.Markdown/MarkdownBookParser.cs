using System.Text.RegularExpressions;
using ConfigurableReader.Core;

namespace ConfigurableReader.Parsers.Markdown;

public partial class MarkdownBookParser : IBookParser
{
    public string FormatName => "Markdown Files";
    public string[] SupportedExtensions => new[] { ".md", ".markdown" };

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    public async Task<IBookSource> CreateSourceAsync(string filePath)
    {
        string text = await ExtractTextAsync(filePath);
        return new MemoryBookSource(text);
    }

    public async Task<string> ExtractTextAsync(string filePath)
    {
        string markdown = await File.ReadAllTextAsync(filePath);
        
        return await Task.Run(() =>
        {
            // Some versions of Markdig have ToPlainText extension
            // If not, we'll have to parse and iterate.
            string plainText = Markdig.Markdown.ToPlainText(markdown);
            return NormalizeWhitespace(plainText);
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
