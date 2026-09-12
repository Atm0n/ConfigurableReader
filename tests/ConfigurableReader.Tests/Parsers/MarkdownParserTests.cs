using ConfigurableReader.Parsers.Markdown;
using Shouldly;

namespace ConfigurableReader.Tests.Parsers;

public class MarkdownParserTests
{
    [Fact]
    public async Task CreateSourceAsync_ReadsAndNormalizesMarkdown()
    {
        // Arrange
        string tempFile = Path.GetTempFileName() + ".md";
        await File.WriteAllTextAsync(tempFile, "# Hello\n\nThis is **Markdown**.", TestContext.Current.CancellationToken);

        var parser = new MarkdownBookParser();

        // Act
        using var source = await parser.CreateSourceAsync(tempFile);
        var text = await source.GetTextAsync(0, source.TotalLength);

        // Assert
        text.ShouldContain("Hello");
        text.ShouldContain("This is Markdown.");

        File.Delete(tempFile);
    }
}
