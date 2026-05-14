using ConfigurableReader.Core;

namespace ConfigurableReader.Parsers.Txt;

public class TxtBookParser : IBookParser
{
    public string FormatName => "Text Files";
    public string[] SupportedExtensions => [".txt"];

    public async Task<string> ExtractTextAsync(string filePath)
    {
        string text = await File.ReadAllTextAsync(filePath);
        
        // Apply existing normalization logic
        return text.Replace("\r", " ")
                   .Replace("\n", " ")
                   .Replace("  ", " ");
    }
}
