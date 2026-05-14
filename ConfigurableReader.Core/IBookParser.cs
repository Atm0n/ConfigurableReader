namespace ConfigurableReader.Core;

public interface IBookParser
{
    string FormatName { get; }
    string[] SupportedExtensions { get; }
    Task<string> ExtractTextAsync(string filePath);
}
