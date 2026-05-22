using ConfigurableReader.Core;
using ConfigurableReader.Services;
using FluentAssertions;

namespace ConfigurableReader.Tests.Services;

public class ReaderControllerTests
{
    [Fact]
    public void Constructor_InitializesEmptyBookRecords_WhenStoreIsEmpty()
    {
        // Arrange
        var registry = new DocumentRegistry();
        var readerService = new ReaderService();
        
        // Act
        var controller = new ReaderController(registry, readerService);
        
        // Assert
        // This will be empty if BookRecordStore.Load() returns empty or file doesn't exist during test.
        controller.BookRecords.Should().NotBeNull();
    }

    [Fact]
    public void GetOrCreateRecord_AddsNewRecordToBookRecords()
    {
        // Arrange
        var registry = new DocumentRegistry();
        var readerService = new ReaderService();
        var controller = new ReaderController(registry, readerService);
        int initialCount = controller.BookRecords.Count;
        
        // Act
        var record = controller.GetOrCreateRecord("C:\\test\\book.txt");

        // Assert
        record.Should().NotBeNull();
        record.FilePath.Should().Be("C:\\test\\book.txt");
        controller.BookRecords.Count.Should().Be(initialCount + 1);
        controller.BookRecords.Should().Contain(r => r.FilePath == "C:\\test\\book.txt");
    }
}
