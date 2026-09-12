using ConfigurableReader.Core;
using ConfigurableReader.Services;
using Moq;
using Shouldly;

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
        service.CurrentPosition.ShouldBe(100);
        service.TotalLength.ShouldBe(1000);
        service.BufferText.ShouldBe("Sample text buffer");
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
        service.CurrentPosition.ShouldBe(105);
    }
}
