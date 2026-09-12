using ConfigurableReader.Core;
using ConfigurableReader.Parsers.Html;
using Moq;
using Moq.Protected;
using Shouldly;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ConfigurableReader.Tests.Parsers;

public class HtmlBookParserTests
{
    [Fact]
    public void SupportedExtensions_ShouldIncludeHtmlAndHtm()
    {
        var parser = new HtmlBookParser();
        parser.SupportedExtensions.ShouldContain(".html");
        parser.SupportedExtensions.ShouldContain(".htm");
        parser.FormatName.ShouldBe("Web & HTML Files");
    }

    [Fact]
    public void HtmlArticleExtractor_ShouldExtractOgTitle()
    {
        string html = """
            <!DOCTYPE html>
            <html>
            <head>
                <meta property="og:title" content="OpenGraph Article Title" />
                <title>Fallback Title</title>
            </head>
            <body>
                <article>
                    <p>This is the main article content.</p>
                </article>
            </body>
            </html>
            """;

        var article = HtmlArticleExtractor.Extract(html);
        article.Title.ShouldBe("OpenGraph Article Title");
        article.CleanText.ShouldContain("This is the main article content.");
    }

    [Fact]
    public void HtmlArticleExtractor_ShouldStripScriptsStylesAndAds()
    {
        string html = """
            <html>
            <head>
                <title>Test Page</title>
                <style>body { background: red; }</style>
            </head>
            <body>
                <script>console.log('unwanted');</script>
                <nav><a href="/">Home</a><a href="/about">About</a></nav>
                <div class="ad-banner">Buy our product now!</div>
                <article>
                    <p>Clean reading text that should remain.</p>
                </article>
                <footer>Copyright 2026</footer>
            </body>
            </html>
            """;

        var article = HtmlArticleExtractor.Extract(html);
        article.CleanText.ShouldContain("Clean reading text that should remain.");
        article.CleanText.ShouldNotContain("console.log");
        article.CleanText.ShouldNotContain("background: red");
        article.CleanText.ShouldNotContain("Buy our product now!");
        article.CleanText.ShouldNotContain("Copyright 2026");
    }

    [Fact]
    public void HtmlArticleExtractor_ShouldExtractHeadingsIntoTableOfContents()
    {
        string html = """
            <html>
            <head><title>Document</title></head>
            <body>
                <article>
                    <h1>Introduction to Speed Reading</h1>
                    <p>Some introductory paragraph about reading faster.</p>
                    <h2>The Spritz Method</h2>
                    <p>Details about rapid serial visual presentation.</p>
                    <h2>Summary</h2>
                    <p>Concluding thoughts.</p>
                </article>
            </body>
            </html>
            """;

        var article = HtmlArticleExtractor.Extract(html);
        article.Headings.Count.ShouldBe(3);
        article.Headings[0].Title.ShouldBe("Introduction to Speed Reading");
        article.Headings[1].Title.ShouldBe("The Spritz Method");
        article.Headings[2].Title.ShouldBe("Summary");

        // Heading positions should be monotonically increasing
        article.Headings[0].Position.ShouldBeLessThan(article.Headings[1].Position);
        article.Headings[1].Position.ShouldBeLessThan(article.Headings[2].Position);
    }

    [Fact]
    public async Task HtmlBookParser_ShouldParseLocalHtmlFile()
    {
        string tempFile = Path.GetTempFileName() + ".html";
        try
        {
            string html = """
                <html>
                <head><title>Local Article</title></head>
                <body>
                    <article>
                        <h1>Local File Reading</h1>
                        <p>Testing local HTML file loading in ConfigurableReader.</p>
                    </article>
                </body>
                </html>
                """;
            await File.WriteAllTextAsync(tempFile, html, TestContext.Current.CancellationToken);

            var parser = new HtmlBookParser();
            using var source = await parser.CreateSourceAsync(tempFile);

            source.TotalLength.ShouldBeGreaterThan(0);
            source.Title.ShouldBe("Local Article");
            string text = await source.GetTextAsync(0, source.TotalLength);
            text.ShouldContain("Local File Reading");
            text.ShouldContain("Testing local HTML file loading");
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task HtmlBookParser_ShouldFetchWebArticleViaHttpClient()
    {
        string mockHtml = """
            <html>
            <head>
                <meta property="og:title" content="Quantum Computing Breakthrough" />
            </head>
            <body>
                <article>
                    <h1>Breakthrough in Quantum Entanglement</h1>
                    <p>Scientists have achieved a new benchmark in quantum coherence.</p>
                </article>
            </body>
            </html>
            """;

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(mockHtml)
            });

        var client = new HttpClient(handlerMock.Object);
        var parser = new HtmlBookParser(client);

        using var source = await parser.CreateSourceAsync("https://science.org/quantum-article");

        source.Title.ShouldBe("Quantum Computing Breakthrough");
        source.TotalLength.ShouldBeGreaterThan(0);
        string text = await source.GetTextAsync(0, source.TotalLength);
        text.ShouldContain("Scientists have achieved a new benchmark in quantum coherence.");
    }

    [Fact]
    public void DocumentRegistry_ShouldRouteUrlsAndHtmlToHtmlParser()
    {
        var registry = new DocumentRegistry();
        var htmlParser = new HtmlBookParser();
        registry.RegisterParser(htmlParser);

        registry.GetParserForFile("https://example.com/article").ShouldBe(htmlParser);
        registry.GetParserForFile("http://news.ycombinator.com").ShouldBe(htmlParser);
        registry.GetParserForFile("my_article.html").ShouldBe(htmlParser);
        registry.GetParserForFile("page.htm").ShouldBe(htmlParser);
        registry.GetParserForFile("book.txt").ShouldBeNull();
    }
}
