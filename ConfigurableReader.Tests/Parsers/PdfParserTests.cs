using ConfigurableReader.Parsers.Pdf;
using FluentAssertions;

namespace ConfigurableReader.Tests.Parsers;

public class PdfParserTests
{
    [Fact]
    public void FormatName_IsPdf()
    {
        var parser = new PdfBookParser();
        parser.FormatName.Should().Be("PDF Documents");
    }

    [Fact]
    public void SupportedExtensions_ContainsPdf()
    {
        var parser = new PdfBookParser();
        parser.SupportedExtensions.Should().Contain(".pdf");
    }
}
