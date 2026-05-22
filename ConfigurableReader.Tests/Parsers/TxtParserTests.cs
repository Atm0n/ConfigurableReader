using System;
using System.IO;
using System.Threading.Tasks;
using ConfigurableReader.Parsers.Txt;
using FluentAssertions;
using Xunit;

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
        parser.FormatName.Should().Be("Text Files");
    }

    [Fact]
    public void TxtBookParser_SupportedExtensions_ContainsTxt()
    {
        var parser = new TxtBookParser();
        parser.SupportedExtensions.Should().Contain(".txt");
    }

    [Fact]
    public async Task CreateSourceAsync_WithValidFile_ReturnsTxtBookSource()
    {
        // Arrange
        await File.WriteAllTextAsync(_tempFilePath, "Hello World");
        var parser = new TxtBookParser();

        // Act
        using var source = await parser.CreateSourceAsync(_tempFilePath);

        // Assert
        source.Should().NotBeNull();
        source.Should().BeOfType<TxtBookSource>();
    }

    [Fact]
    public async Task TxtBookSource_TotalLength_MatchesFileLength()
    {
        // Arrange
        var content = "This is a test document.";
        await File.WriteAllTextAsync(_tempFilePath, content);
        var parser = new TxtBookParser();
        using var source = await parser.CreateSourceAsync(_tempFilePath);

        // Act
        var length = source.TotalLength;

        // Assert
        length.Should().Be(content.Length);
    }

    [Fact]
    public async Task TxtBookSource_GetTextAsync_ReturnsCorrectTextAndReplacesNewlines()
    {
        // Arrange
        var content = "Line 1\r\nLine 2\tTabbed";
        await File.WriteAllTextAsync(_tempFilePath, content);
        var parser = new TxtBookParser();
        using var source = await parser.CreateSourceAsync(_tempFilePath);

        // Act
        var text = await source.GetTextAsync(0, source.TotalLength);

        // Assert
        text.Should().Be("Line 1  Line 2 Tabbed");
    }

    [Fact]
    public async Task TxtBookSource_GetTextAsync_WithStartBeyondLength_ReturnsEmptyString()
    {
        // Arrange
        var content = "Short";
        await File.WriteAllTextAsync(_tempFilePath, content);
        var parser = new TxtBookParser();
        using var source = await parser.CreateSourceAsync(_tempFilePath);

        // Act
        var text = await source.GetTextAsync(100, 10);

        // Assert
        text.Should().BeEmpty();
    }
}
