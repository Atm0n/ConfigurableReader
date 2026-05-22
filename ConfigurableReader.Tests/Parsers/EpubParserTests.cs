using ConfigurableReader.Parsers.Epub;
using FluentAssertions;

namespace ConfigurableReader.Tests.Parsers;

public class EpubParserTests
{
    [Fact]
    public void FormatName_IsEpub()
    {
        var parser = new EpubBookParser();
        parser.FormatName.Should().Be("EPUB Books");
    }

    [Fact]
    public void SupportedExtensions_ContainsEpub()
    {
        var parser = new EpubBookParser();
        parser.SupportedExtensions.Should().Contain(".epub");
    }
}
