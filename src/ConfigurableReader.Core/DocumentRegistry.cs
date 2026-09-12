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
        if (string.IsNullOrWhiteSpace(filePath)) return null;

        if (filePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            filePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return _parsers.FirstOrDefault(p => p.SupportedExtensions.Contains(".html"));
        }

        string extension = Path.GetExtension(filePath).ToLowerInvariant();
        return _parsers.FirstOrDefault(p => p.SupportedExtensions.Contains(extension));
    }

    public IEnumerable<IBookParser> AvailableParsers => _parsers;

    public async Task<IBookSource> CreateSourceAsync(string filePath)
    {
        var parser = GetParserForFile(filePath);
        if (parser == null)
        {
            throw new NotSupportedException($"No parser found for file extension: {Path.GetExtension(filePath)}");
        }

        return await parser.CreateSourceAsync(filePath);
    }
}
