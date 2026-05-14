using System.Text;
using System.Text.RegularExpressions;
using ConfigurableReader.Core;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace ConfigurableReader.Parsers.Docx;

public partial class DocxBookParser : IBookParser
{
    public string FormatName => "Word Documents";
    public string[] SupportedExtensions => new[] { ".docx" };

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    public async Task<string> ExtractTextAsync(string filePath)
    {
        return await Task.Run(() =>
        {
            StringBuilder sb = new();
            using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(filePath, false))
            {
                var body = wordDoc.MainDocumentPart?.Document.Body;
                if (body != null)
                {
                    foreach (var paragraph in body.Descendants<Paragraph>())
                    {
                        string text = paragraph.InnerText;
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            sb.Append(text);
                            sb.Append(' ');
                        }
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
