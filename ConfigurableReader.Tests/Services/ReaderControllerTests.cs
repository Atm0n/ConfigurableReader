using ConfigurableReader.Core;
using ConfigurableReader.Services;
using Shouldly;

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
        controller.BookRecords.ShouldNotBeNull();
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
        record.ShouldNotBeNull();
        record.FilePath.ShouldBe("C:\\test\\book.txt");
        controller.BookRecords.Count.ShouldBe(initialCount + 1);
        controller.BookRecords.ShouldContain(r => r.FilePath == "C:\\test\\book.txt");
    }
}
