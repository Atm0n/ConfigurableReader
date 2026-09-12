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

    [Fact]
    public void ExtractTextFromHtml_RemovesScriptsAndInsertsBlockSpacing()
    {
        string html = "<html><head><style>body { color: red; }</style><script>alert(1);</script></head><body><h1>Chapter 1</h1><p>First paragraph.</p><p>Second paragraph.</p></body></html>";
        string extracted = EpubBookParser.ExtractTextFromHtml(html);
        string normalized = EpubBookParser.NormalizeWhitespace(extracted);

        normalized.ShouldBe("Chapter 1 First paragraph. Second paragraph.");
    }
}
