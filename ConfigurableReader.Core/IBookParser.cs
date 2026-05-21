namespace ConfigurableReader.Core;

/// <summary>
/// Provides a virtualized view of a book's content without requiring the full text to be in memory.
/// </summary>
public interface IBookSource : IDisposable
{
    /// <summary>
    /// The total estimated length of the book in characters.
    /// </summary>
    int TotalLength { get; }

    /// <summary>
    /// Retrieves a specific range of text from the book.
    /// </summary>
    /// <param name="start">The starting character position.</param>
    /// <param name="count">The number of characters to retrieve.</param>
    /// <returns>A string containing the requested range.</returns>
    Task<string> GetTextAsync(int start, int count);
}

public interface IBookParser
{
    string FormatName { get; }
    string[] SupportedExtensions { get; }
    
    /// <summary>
    /// Creates a source that can provide text chunks for the book.
    /// </summary>
    Task<IBookSource> CreateSourceAsync(string filePath);
}
