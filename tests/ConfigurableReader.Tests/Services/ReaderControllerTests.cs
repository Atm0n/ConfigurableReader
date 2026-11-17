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

    [Fact]
    public void BookRecord_ProgressPercentageAndFormattedProgress_CalculatesCorrectly()
    {
        var record = new global::ConfigurableReader.Models.BookRecord
        {
            ScrollPosition = 250,
            TotalLength = 1000
        };

        record.ProgressPercentage.ShouldBe(25.0);
        record.FormattedProgress.ShouldBe($"{25.0:F1}%");
    }

    [Fact]
    public void BookRecord_FormattedProgress_WhenTotalLengthZero_ReturnsZeroPercent()
    {
        var record = new global::ConfigurableReader.Models.BookRecord
        {
            ScrollPosition = 0,
            TotalLength = 0
        };

        record.ProgressPercentage.ShouldBe(0);
        record.FormattedProgress.ShouldBe("0%");
    }

    [Fact]
    public async Task OpenBookAsync_WhenParsingFails_DoesNotChangeCurrentBookOrCreateRecord()
    {
        // Arrange: registry with no parsers, so any open throws NotSupportedException
        var registry = new DocumentRegistry();
        var readerService = new ReaderService();
        var controller = new ReaderController(registry, readerService);
        const string badPath = @"C:\test\unsupported.xyz";
        int initialCount = controller.BookRecords.Count;

        // Act
        await Should.ThrowAsync<NotSupportedException>(() => controller.OpenBookAsync(badPath));

        // Assert
        controller.CurrentBookFilePath.ShouldBeNull();
        controller.CurrentRecord.ShouldBeNull();
        controller.BookRecords.Count.ShouldBe(initialCount);
        controller.BookRecords.ShouldNotContain(r => r.FilePath == badPath);
    }

    [Fact]
    public void PathsEqual_IgnoresCaseForLocalPathsOnlyOnWindowsAndMac()
    {
        bool expected = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS();
        ReaderController.PathsEqual(@"C:\Books\Novel.epub", @"c:\books\novel.epub").ShouldBe(expected);
        ReaderController.PathsEqual(@"C:\Books\Novel.epub", @"C:\Books\Novel.epub").ShouldBeTrue();
    }

    [Fact]
    public void PathsEqual_UrlsAreCaseSensitive()
    {
        ReaderController.PathsEqual("https://example.com/Article", "https://example.com/article").ShouldBeFalse();
        ReaderController.PathsEqual(null, null).ShouldBeTrue();
        ReaderController.PathsEqual("a.txt", null).ShouldBeFalse();
    }
}
