using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ConfigurableReader.Core;

namespace ConfigurableReader.Parsers.Txt;

public partial class TxtBookParser : IBookParser
{
    public string FormatName => "Text Files";
    public string[] SupportedExtensions => new[] { ".txt" };

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    public async Task<string> ExtractTextAsync(string filePath)
    {
        string text = await File.ReadAllTextAsync(filePath);
        
        // Normalize whitespace: replace all types of whitespace with single spaces
        string step1 = text.Replace("\r", " ").Replace("\n", " ").Replace("\t", " ");
        return WhitespaceRegex().Replace(step1, " ").Trim();
    }
}
