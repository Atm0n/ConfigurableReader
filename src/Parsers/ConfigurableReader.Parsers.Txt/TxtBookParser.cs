using ConfigurableReader.Core;

namespace ConfigurableReader.Parsers.Txt;

public partial class TxtBookParser : IBookParser
{
    public string FormatName => "Text Files";
    public string[] SupportedExtensions => [".txt"];

    public async Task<IBookSource> CreateSourceAsync(string filePath)
    {
        return await TxtBookSource.CreateAsync(filePath);
    }
}
