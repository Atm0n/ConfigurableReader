using ConfigurableReader.Core;
using ConfigurableReader.Services;
using FluentAssertions;
using Moq;

namespace ConfigurableReader.Tests.Services;

public class ReaderServiceTests
{
    [Fact]
    public async Task SetSourceAsync_InitializesCorrectly()
    {
        // Arrange
        var service = new ReaderService();
        var mockSource = new Mock<IBookSource>();
        mockSource.Setup(s => s.TotalLength).Returns(1000);
        mockSource.Setup(s => s.GetTextAsync(It.IsAny<int>(), It.IsAny<int>()))
                  .ReturnsAsync("Sample text buffer");

        // Act
        await service.SetSourceAsync(mockSource.Object, 100);

        // Assert
        service.CurrentPosition.Should().Be(100);
        service.TotalLength.Should().Be(1000);
        service.BufferText.Should().Be("Sample text buffer");
    }

    [Fact]
    public async Task Advance_UpdatesPosition_WhenNotPaused()
    {
        // Arrange
        var service = new ReaderService();
        var mockSource = new Mock<IBookSource>();
        mockSource.Setup(s => s.TotalLength).Returns(1000);
        mockSource.Setup(s => s.GetTextAsync(It.IsAny<int>(), It.IsAny<int>()))
                  .ReturnsAsync("Sample text buffer");

        await service.SetSourceAsync(mockSource.Object, 100);
        service.IsPaused = false; // Unpause

        // Act
        service.Advance(10.0, (pos, offset) => 
        {
            // Dummy mapping function that advances by 5 chars
            return (pos + 5, 0.0, false);
        });

        // Assert
        service.CurrentPosition.Should().Be(105);
    }
}
