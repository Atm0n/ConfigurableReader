namespace ConfigurableReader.Core;

public class DocumentRegistry
{
    private readonly List<IBookParser> _parsers = [];

    public void RegisterParser(IBookParser parser)
    {
        _parsers.Add(parser);
    }

    public IBookParser? GetParserForFile(string filePath)
    {
        string extension = Path.GetExtension(filePath).ToLowerInvariant();
        return _parsers.FirstOrDefault(p => p.SupportedExtensions.Contains(extension));
    }

    public IEnumerable<IBookParser> AvailableParsers => _parsers;

    public async Task<string> LoadBookAsync(string filePath)
    {
        var parser = GetParserForFile(filePath);
        if (parser == null)
        {
            throw new NotSupportedException($"No parser found for file extension: {Path.GetExtension(filePath)}");
        }

        return await parser.ExtractTextAsync(filePath);
    }
}
