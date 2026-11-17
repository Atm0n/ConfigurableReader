using ConfigurableReader.Core;

namespace ConfigurableReader.Parsers.Html;

public partial class HtmlBookParser : IBookParser
{
    public string FormatName => "Web & HTML Files";
    public string[] SupportedExtensions => [".html", ".htm"];

    private readonly HttpClient _httpClient;

    public HtmlBookParser(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(20),
            DefaultRequestHeaders =
            {
                { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36 ConfigurableReader/1.1" },
                { "Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8" },
                { "Accept-Language", "en-US,en;q=0.9,es;q=0.8" }
            }
        };
    }

    public async Task<IBookSource> CreateSourceAsync(string filePathOrUrl)
    {
        string rawHtml;
        string? fallbackTitle;

        if (filePathOrUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            filePathOrUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            fallbackTitle = GetTitleFromUrl(filePathOrUrl);
            rawHtml = await _httpClient.GetStringAsync(filePathOrUrl);
        }
        else
        {
            fallbackTitle = Path.GetFileNameWithoutExtension(filePathOrUrl);
            rawHtml = await File.ReadAllTextAsync(filePathOrUrl);
        }

        var article = HtmlArticleExtractor.Extract(rawHtml, fallbackTitle);
        return new MemoryBookSource(article.CleanText, article.Headings, article.Title);
    }

    public async Task<byte[]?> ExtractCoverImageAsync(string filePathOrUrl)
    {
        try
        {
            string rawHtml;
            if (filePathOrUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                filePathOrUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                rawHtml = await _httpClient.GetStringAsync(filePathOrUrl);
            }
            else
            {
                rawHtml = await File.ReadAllTextAsync(filePathOrUrl);
            }

            var article = HtmlArticleExtractor.Extract(rawHtml);
            if (!string.IsNullOrEmpty(article.CoverImageUrl))
            {
                return await DownloadCoverAsync(article.CoverImageUrl);
            }
        }
        catch
        {
            // Fallback to null
        }
        return null;
    }

    /// <summary>Upper bound for a downloaded cover image; larger responses are discarded.</summary>
    public const int MaxCoverBytes = 5 * 1024 * 1024;

    private static readonly TimeSpan CoverDownloadTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Downloads a cover image with its own short timeout and a size cap, so a huge or slow
    /// og:image can't stall the library or fill memory. Returns null if either limit is hit.
    /// </summary>
    private async Task<byte[]?> DownloadCoverAsync(string imageUrl)
    {
        using var cts = new CancellationTokenSource(CoverDownloadTimeout);
        using var response = await _httpClient.GetAsync(imageUrl, HttpCompletionOption.ResponseHeadersRead, cts.Token);
        if (!response.IsSuccessStatusCode) return null;

        if (response.Content.Headers.ContentLength is long declared && declared > MaxCoverBytes)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cts.Token);
        using var buffer = new MemoryStream();
        byte[] chunk = new byte[81920];
        int read;
        while ((read = await stream.ReadAsync(chunk, cts.Token)) > 0)
        {
            if (buffer.Length + read > MaxCoverBytes)
            {
                return null;
            }
            buffer.Write(chunk, 0, read);
        }

        return buffer.Length > 0 ? buffer.ToArray() : null;
    }

    private static string GetTitleFromUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            string segment = uri.Segments.Length > 0 ? uri.Segments[^1].TrimEnd('/') : string.Empty;
            if (!string.IsNullOrEmpty(segment))
            {
                return segment.Replace('-', ' ').Replace('_', ' ');
            }
            return uri.Host;
        }
        catch
        {
            return url;
        }
    }
}
