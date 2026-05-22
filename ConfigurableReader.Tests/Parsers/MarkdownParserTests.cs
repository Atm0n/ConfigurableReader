using ConfigurableReader.Parsers.Markdown;
using FluentAssertions;

namespace ConfigurableReader.Tests.Parsers;

public class MarkdownParserTests
{
    [Fact]
    public async Task CreateSourceAsync_ReadsAndNormalizesMarkdown()
    {
        // Arrange
        string tempFile = Path.GetTempFileName() + ".md";
        await File.WriteAllTextAsync(tempFile, "# Hello\n\nThis is **Markdown**.");
        
        var parser = new MarkdownBookParser();

        // Act
        using var source = await parser.CreateSourceAsync(tempFile);
        var text = await source.GetTextAsync(0, source.TotalLength);

        // Assert
        text.Should().Contain("Hello");
        text.Should().Contain("This is Markdown.");
        
        File.Delete(tempFile);
    }
}
