using ConfigurableReader.Parsers.Epub;
using Shouldly;

namespace ConfigurableReader.Tests.Parsers;

public class EpubParserTests
{
    [Fact]
    public void FormatName_IsEpub()
    {
        var parser = new EpubBookParser();
        parser.FormatName.ShouldBe("EPUB Books");
    }

    [Fact]
    public void SupportedExtensions_ContainsEpub()
    {
        var parser = new EpubBookParser();
        parser.SupportedExtensions.ShouldContain(".epub");
    }
}
