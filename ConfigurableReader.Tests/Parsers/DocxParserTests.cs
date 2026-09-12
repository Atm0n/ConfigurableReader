using ConfigurableReader.Parsers.Docx;
using Shouldly;

namespace ConfigurableReader.Tests.Parsers;

public class DocxParserTests
{
    [Fact]
    public void FormatName_IsDocx()
    {
        var parser = new DocxBookParser();
        parser.FormatName.ShouldBe("Word Documents");
    }

    [Fact]
    public void SupportedExtensions_ContainsDocx()
    {
        var parser = new DocxBookParser();
        parser.SupportedExtensions.ShouldContain(".docx");
    }
}
