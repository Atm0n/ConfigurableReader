using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ConfigurableReader.Core;

namespace ConfigurableReader.Parsers.Txt;

public partial class TxtBookParser : IBookParser
{
    public string FormatName => "Text Files";
    public string[] SupportedExtensions => new[] { ".txt" };

    [GeneratedRegex(@"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]")]
    private static partial Regex ControlCharsRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    public async Task<string> ExtractTextAsync(string filePath)
    {
        using var reader = new StreamReader(filePath, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        string text = await reader.ReadToEndAsync();
        
        text = ControlCharsRegex().Replace(text, "");
        text = WhitespaceRegex().Replace(text, " ");

        return text.Trim();
    }
}
