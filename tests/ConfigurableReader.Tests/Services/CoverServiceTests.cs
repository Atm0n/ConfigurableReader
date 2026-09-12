using ConfigurableReader.Core;
using ConfigurableReader.Models;
using ConfigurableReader.Services;
using Moq;
using Shouldly;

namespace ConfigurableReader.Tests.Services;

public class CoverServiceTests
{
    [Fact]
    public void GetCoverCachePath_ShouldGenerateDeterministicPath()
    {
        string path1 = CoverService.GetCoverCachePath("C:\\Books\\GreatBook.epub");
        string path2 = CoverService.GetCoverCachePath("c:\\books\\greatbook.epub");

        path1.ShouldBe(path2);
        path1.ShouldEndWith(".png");
        Path.GetFileName(path1).Length.ShouldBeGreaterThan(5);
    }

    [Fact]
    public async Task EnsureCoverExtractedAsync_CachesParserBytes()
    {
        var mockParser = new Mock<IBookParser>();
        byte[] fakeCoverBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]; // PNG magic header

        mockParser.Setup(p => p.SupportedExtensions).Returns([".test"]);
        mockParser.Setup(p => p.ExtractCoverImageAsync("test_file.test"))
                  .ReturnsAsync(fakeCoverBytes);

        var registry = new DocumentRegistry();
        registry.RegisterParser(mockParser.Object);

        var coverService = new CoverService(registry);
        string? cachedPath = await coverService.EnsureCoverExtractedAsync("test_file.test");

        cachedPath.ShouldNotBeNull();
        File.Exists(cachedPath).ShouldBeTrue();

        byte[] readBytes = await File.ReadAllBytesAsync(cachedPath, TestContext.Current.CancellationToken);
        readBytes.ShouldBe(fakeCoverBytes);

        // Cleanup
        try { File.Delete(cachedPath); } catch { }
    }

    [Fact]
    public void BookRecord_CoverProperties_ShouldWorkAsExpected()
    {
        var record = new BookRecord
        {
            FilePath = "https://techcrunch.com/article",
            Title = "Tech Article"
        };

        record.IsWebArticle.ShouldBeTrue();
        record.CoverIcon.ShouldBe("🌐");
        record.HasCoverBitmap.ShouldBeFalse();

        var localRecord = new BookRecord
        {
            FilePath = "C:\\Books\\Fantasy.epub"
        };

        localRecord.IsWebArticle.ShouldBeFalse();
        localRecord.CoverIcon.ShouldBe("📖");
    }

    [Fact]
    public void BookRecord_PropertyChanged_FiresOnCoverBitmapChanged()
    {
        var record = new BookRecord { FilePath = "test.epub" };
        bool coverBitmapChanged = false;
        bool hasCoverChanged = false;

        record.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(BookRecord.CoverBitmap)) coverBitmapChanged = true;
            if (e.PropertyName == nameof(BookRecord.HasCoverBitmap)) hasCoverChanged = true;
        };

        record.CoverBitmap = null;
        // Setting to null when already null should not fire
        coverBitmapChanged.ShouldBeFalse();
        hasCoverChanged.ShouldBeFalse();
    }
}
