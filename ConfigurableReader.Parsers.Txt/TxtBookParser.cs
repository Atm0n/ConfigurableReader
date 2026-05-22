using ConfigurableReader.Core;

namespace ConfigurableReader.Parsers.Txt;

public partial class TxtBookParser : IBookParser
{
    public string FormatName => "Text Files";
    public string[] SupportedExtensions => new[] { ".txt" };

    public async Task<IBookSource> CreateSourceAsync(string filePath)
    {
        return await Task.FromResult(new TxtBookSource(filePath));
    }
}
