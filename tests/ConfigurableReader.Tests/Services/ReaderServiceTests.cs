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

    [Fact]
    public async Task AdvanceToPosition_UpdatesPositionAndHandlesEof()
    {
        // Arrange
        var service = new ReaderService();
        var mockSource = new Mock<IBookSource>();
        mockSource.Setup(s => s.TotalLength).Returns(500);
        mockSource.Setup(s => s.GetTextAsync(It.IsAny<int>(), It.IsAny<int>()))
                  .ReturnsAsync("Sample text buffer");

        await service.SetSourceAsync(mockSource.Object, 100);
        service.IsPaused = false;

        // Act: Advance within bounds
        service.AdvanceToPosition(250);
        service.CurrentPosition.ShouldBe(250);
        service.IsPaused.ShouldBeFalse();

        // Act: Advance to EOF
        bool eofFired = false;
        service.EndOfBookReached += () => eofFired = true;

        service.AdvanceToPosition(500);
        service.CurrentPosition.ShouldBe(500);
        service.IsPaused.ShouldBeTrue();
        eofFired.ShouldBeTrue();
    }

    [Fact]
    public async Task Advance_NearBufferEdge_StartsOnlyOneBackgroundSlideAtATime()
    {
        // Arrange: initial load returns immediately; later loads block until released.
        var service = new ReaderService();
        var release = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        int calls = 0;
        var mockSource = new Mock<IBookSource>();
        mockSource.Setup(s => s.TotalLength).Returns(200_000);
        mockSource.Setup(s => s.GetTextAsync(It.IsAny<int>(), It.IsAny<int>()))
                  .Returns(() => ++calls == 1 ? Task.FromResult(new string('a', 50_000)) : release.Task);

        await service.SetSourceAsync(mockSource.Object, 0);
        service.IsPaused = false;

        // Act: several frames close to the end of the 50k buffer while the slide is in flight.
        for (int i = 0; i < 5; i++)
        {
            service.Advance(1.0, (pos, _) => (45_000 + i, 0.0, false));
        }

        // Assert: one initial load + exactly one slide.
        calls.ShouldBe(2);

        release.SetResult(new string('b', 50_000));
        await service.PendingBufferSlide.ShouldNotBeNull();
        service.BufferStartPosition.ShouldBe(45_000 - 25_000);
    }
}
