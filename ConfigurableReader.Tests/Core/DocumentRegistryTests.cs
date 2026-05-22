using System;
using System.IO;
using System.Threading.Tasks;
using ConfigurableReader.Core;
using FluentAssertions;
using Moq;
using Xunit;

namespace ConfigurableReader.Tests.Core;

public class DocumentRegistryTests
{
    [Fact]
    public void GetParserForFile_WhenParserRegistered_ReturnsCorrectParser()
    {
        // Arrange
        var registry = new DocumentRegistry();
        var mockParser = new Mock<IBookParser>();
        mockParser.Setup(p => p.SupportedExtensions).Returns(new[] { ".txt" });
        
        registry.RegisterParser(mockParser.Object);

        // Act
        var result = registry.GetParserForFile("book.txt");

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(mockParser.Object);
    }

    [Fact]
    public void GetParserForFile_WhenNoParserRegisteredForExtension_ReturnsNull()
    {
        // Arrange
        var registry = new DocumentRegistry();
        var mockParser = new Mock<IBookParser>();
        mockParser.Setup(p => p.SupportedExtensions).Returns(new[] { ".pdf" });
        
        registry.RegisterParser(mockParser.Object);

        // Act
        var result = registry.GetParserForFile("book.txt");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateSourceAsync_WhenParserExists_ReturnsSource()
    {
        // Arrange
        var registry = new DocumentRegistry();
        
        var mockSource = new Mock<IBookSource>();
        var mockParser = new Mock<IBookParser>();
        
        mockParser.Setup(p => p.SupportedExtensions).Returns(new[] { ".txt" });
        mockParser.Setup(p => p.CreateSourceAsync(It.IsAny<string>()))
                  .ReturnsAsync(mockSource.Object);
        
        registry.RegisterParser(mockParser.Object);

        // Act
        var result = await registry.CreateSourceAsync("book.txt");

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(mockSource.Object);
        mockParser.Verify(p => p.CreateSourceAsync("book.txt"), Times.Once);
    }

    [Fact]
    public async Task CreateSourceAsync_WhenNoParserExists_ThrowsNotSupportedException()
    {
        // Arrange
        var registry = new DocumentRegistry();

        // Act
        Func<Task> act = async () => await registry.CreateSourceAsync("book.unknown");

        // Assert
        await act.Should().ThrowAsync<NotSupportedException>()
                 .WithMessage("No parser found for file extension: .unknown");
    }
}
