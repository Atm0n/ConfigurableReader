using ConfigurableReader.Parsers.Docx;
using FluentAssertions;

namespace ConfigurableReader.Tests.Parsers;

public class DocxParserTests
{
    [Fact]
    public void FormatName_IsDocx()
    {
        var parser = new DocxBookParser();
        parser.FormatName.Should().Be("Word Documents");
    }

    [Fact]
    public void SupportedExtensions_ContainsDocx()
    {
        var parser = new DocxBookParser();
        parser.SupportedExtensions.Should().Contain(".docx");
    }
}
