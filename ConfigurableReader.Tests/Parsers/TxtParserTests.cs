using ConfigurableReader.Parsers.Txt;
using Shouldly;

namespace ConfigurableReader.Tests.Parsers;

public class TxtParserTests : IDisposable
{
    private readonly string _tempFilePath;

    public TxtParserTests()
    {
        _tempFilePath = Path.GetTempFileName() + ".txt";
    }

    public void Dispose()
    {
        if (File.Exists(_tempFilePath))
        {
            File.Delete(_tempFilePath);
        }
    }

    [Fact]
    public void TxtBookParser_FormatName_ReturnsTextFiles()
    {
        var parser = new TxtBookParser();
        parser.FormatName.ShouldBe("Text Files");
    }

    [Fact]
    public void TxtBookParser_SupportedExtensions_ContainsTxt()
    {
        var parser = new TxtBookParser();
        parser.SupportedExtensions.ShouldContain(".txt");
    }

    [Fact]
    public async Task CreateSourceAsync_WithValidFile_ReturnsTxtBookSource()
    {
        // Arrange
        await File.WriteAllTextAsync(_tempFilePath, "Hello World", TestContext.Current.CancellationToken);
        var parser = new TxtBookParser();

        // Act
        using var source = await parser.CreateSourceAsync(_tempFilePath);

        // Assert
        source.ShouldNotBeNull();
        source.ShouldBeOfType<TxtBookSource>();
    }

    [Fact]
    public async Task TxtBookSource_TotalLength_MatchesFileLength()
    {
        // Arrange
        var content = "This is a test document.";
        await File.WriteAllTextAsync(_tempFilePath, content, TestContext.Current.CancellationToken);
        var parser = new TxtBookParser();
        using var source = await parser.CreateSourceAsync(_tempFilePath);

        // Act
        var length = source.TotalLength;

        // Assert
        length.ShouldBe(content.Length);
    }

    [Fact]
    public async Task TxtBookSource_GetTextAsync_ReturnsCorrectTextAndReplacesNewlines()
    {
        // Arrange
        var content = "Line 1\r\nLine 2\tTabbed";
        await File.WriteAllTextAsync(_tempFilePath, content, TestContext.Current.CancellationToken);
        var parser = new TxtBookParser();
        using var source = await parser.CreateSourceAsync(_tempFilePath);

        // Act
        var text = await source.GetTextAsync(0, source.TotalLength);

        // Assert
        text.ShouldBe("Line 1  Line 2 Tabbed");
    }

    [Fact]
    public async Task TxtBookSource_GetTextAsync_WithStartBeyondLength_ReturnsEmptyString()
    {
        // Arrange
        var content = "Short";
        await File.WriteAllTextAsync(_tempFilePath, content, TestContext.Current.CancellationToken);
        var parser = new TxtBookParser();
        using var source = await parser.CreateSourceAsync(_tempFilePath);

        // Act
        var text = await source.GetTextAsync(100, 10);

        // Assert
        text.ShouldBeEmpty();
    }

    [Fact]
    public async Task TxtBookSource_GetTextAsync_WithMultiByteUtf8Characters_PreservesCharactersAndOffsets()
    {
        // Arrange (contains accented characters, Catalan/Spanish chars, and emoji)
        var content = "¡Hola! ¿Cómo estás? Café y música. 😊";
        await File.WriteAllTextAsync(_tempFilePath, content, TestContext.Current.CancellationToken);
        var parser = new TxtBookParser();
        using var source = await parser.CreateSourceAsync(_tempFilePath);

        // Act & Assert
        source.TotalLength.ShouldBe(content.Length);
        var fullText = await source.GetTextAsync(0, source.TotalLength);
        fullText.ShouldBe(content);

        // Slice specifically inside multi-byte characters
        var slice = await source.GetTextAsync(7, 11);
        slice.ShouldBe("¿Cómo estás");
    }
}
