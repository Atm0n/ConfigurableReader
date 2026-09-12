using ConfigurableReader.Parsers.Pdf;
using Shouldly;

namespace ConfigurableReader.Tests.Parsers;

public class PdfParserTests
{
    [Fact]
    public void FormatName_IsPdf()
    {
        var parser = new PdfBookParser();
        parser.FormatName.ShouldBe("PDF Documents");
    }

    [Fact]
    public void SupportedExtensions_ContainsPdf()
    {
        var parser = new PdfBookParser();
        parser.SupportedExtensions.ShouldContain(".pdf");
    }
}
