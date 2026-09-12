using ConfigurableReader.Core;
using Moq;
using Shouldly;

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
        result.ShouldNotBeNull();
        result.ShouldBe(mockParser.Object);
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
        result.ShouldBeNull();
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
        result.ShouldNotBeNull();
        result.ShouldBe(mockSource.Object);
        mockParser.Verify(p => p.CreateSourceAsync("book.txt"), Times.Once);
    }

    [Fact]
    public async Task CreateSourceAsync_WhenNoParserExists_ThrowsNotSupportedException()
    {
        // Arrange
        var registry = new DocumentRegistry();

        // Act & Assert
        var ex = await Should.ThrowAsync<NotSupportedException>(async () => await registry.CreateSourceAsync("book.unknown"));
        ex.Message.ShouldBe("No parser found for file extension: .unknown");
    }
}
