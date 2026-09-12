using ConfigurableReader.Core;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace ConfigurableReader.Parsers.Html;

public record ExtractedArticle(string Title, string CleanText, IReadOnlyList<BookmarkItem> Headings, string? CoverImageUrl = null);

public static partial class HtmlArticleExtractor
{
    private static readonly HashSet<string> DiscardTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "script", "style", "noscript", "nav", "header", "footer", "aside",
        "form", "button", "input", "select", "textarea", "iframe", "svg",
        "canvas", "dialog", "template", "head"
    };

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    public static ExtractedArticle Extract(string rawHtml, string? fallbackTitle = null)
    {
        if (string.IsNullOrWhiteSpace(rawHtml))
        {
            return new ExtractedArticle(fallbackTitle ?? "Untitled", string.Empty, []);
        }

        var doc = new HtmlDocument();
        doc.LoadHtml(rawHtml);

        // 1. Extract Title
        string title = ExtractTitle(doc) ?? fallbackTitle ?? "Untitled";

        // 2. Extract Cover Image URL (before removing tags)
        string? coverImageUrl = ExtractCoverImageUrl(doc);

        // 3. Remove unwanted tags throughout the document
        RemoveUnwantedElements(doc.DocumentNode);

        // 4. Find the main content container
        HtmlNode contentRoot = FindMainContentNode(doc);

        // 5. Extract structured text and headings (Table of Contents)
        var headings = new List<BookmarkItem>();
        var textBuilder = new StringBuilder();

        ExtractNodeText(contentRoot, textBuilder, headings);

        string finalText = WhitespaceRegex().Replace(textBuilder.ToString(), " ").Trim();

        return new ExtractedArticle(title, finalText, headings, coverImageUrl);
    }

    private static string? ExtractCoverImageUrl(HtmlDocument doc)
    {
        var ogImage = doc.DocumentNode.SelectSingleNode("//meta[@property='og:image']/@content")
                   ?? doc.DocumentNode.SelectSingleNode("//meta[@name='twitter:image']/@content");
        if (ogImage != null)
        {
            string url = ogImage.GetAttributeValue("content", "").Trim();
            if (!string.IsNullOrWhiteSpace(url) && (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
            {
                return url;
            }
        }
        return null;
    }

    private static string? ExtractTitle(HtmlDocument doc)
    {
        // Try OpenGraph title
        var ogTitle = doc.DocumentNode.SelectSingleNode("//meta[@property='og:title']/@content")
                   ?? doc.DocumentNode.SelectSingleNode("//meta[@name='twitter:title']/@content");
        if (ogTitle != null && !string.IsNullOrWhiteSpace(ogTitle.GetAttributeValue("content", null)))
        {
            return WebUtility.HtmlDecode(ogTitle.GetAttributeValue("content", "").Trim());
        }

        // Try <title> tag
        var titleNode = doc.DocumentNode.SelectSingleNode("//title");
        if (titleNode != null && !string.IsNullOrWhiteSpace(titleNode.InnerText))
        {
            return WebUtility.HtmlDecode(titleNode.InnerText.Trim());
        }

        // Try first <h1>
        var h1 = doc.DocumentNode.SelectSingleNode("//h1");
        if (h1 != null && !string.IsNullOrWhiteSpace(h1.InnerText))
        {
            return WebUtility.HtmlDecode(h1.InnerText.Trim());
        }

        return null;
    }

    private static void RemoveUnwantedElements(HtmlNode root)
    {
        var nodesToRemove = new List<HtmlNode>();

        foreach (var node in root.DescendantsAndSelf())
        {
            if (DiscardTags.Contains(node.Name))
            {
                nodesToRemove.Add(node);
                continue;
            }

            // Remove typical clutter (ads, popups, cookie notices, share widgets)
            string classAttr = node.GetAttributeValue("class", "").ToLowerInvariant();
            string idAttr = node.GetAttributeValue("id", "").ToLowerInvariant();

            bool isAdOrNoise =
                classAttr.Contains("ad-") || classAttr.Contains("ads-") || classAttr.Contains("advertisement") ||
                classAttr.Contains("cookie-") || classAttr.Contains("social-share") || classAttr.Contains("share-buttons") ||
                idAttr.Contains("cookie-") || idAttr.Contains("newsletter-") || idAttr.Contains("disclaimer");

            if (isAdOrNoise && !node.Name.Equals("body", StringComparison.OrdinalIgnoreCase))
            {
                nodesToRemove.Add(node);
            }
        }

        foreach (var node in nodesToRemove)
        {
            node.Remove();
        }
    }

    private static HtmlNode FindMainContentNode(HtmlDocument doc)
    {
        // 1. Check for <article>
        var articleNode = doc.DocumentNode.SelectSingleNode("//article");
        if (articleNode != null && articleNode.InnerText.Trim().Length > 150)
        {
            return articleNode;
        }

        // 2. Check for <main> or [role="main"]
        var mainNode = doc.DocumentNode.SelectSingleNode("//main")
                    ?? doc.DocumentNode.SelectSingleNode("//*[@role='main']");
        if (mainNode != null && mainNode.InnerText.Trim().Length > 150)
        {
            return mainNode;
        }

        // 3. Check for common article container classes
        var contentDiv = doc.DocumentNode.SelectSingleNode("//*[contains(@class, 'article-content') or contains(@class, 'post-content') or contains(@class, 'entry-content') or contains(@class, 'story-body')]");
        if (contentDiv != null && contentDiv.InnerText.Trim().Length > 150)
        {
            return contentDiv;
        }

        // 4. Fallback to <body> or root
        return doc.DocumentNode.SelectSingleNode("//body") ?? doc.DocumentNode;
    }

    private static void ExtractNodeText(HtmlNode node, StringBuilder sb, List<BookmarkItem> headings)
    {
        string name = node.Name.ToLowerInvariant();

        bool isHeading = name is "h1" or "h2" or "h3" or "h4" or "h5" or "h6";
        if (isHeading)
        {
            string headingText = WebUtility.HtmlDecode(node.InnerText).Trim();
            if (!string.IsNullOrWhiteSpace(headingText))
            {
                int position = sb.Length > 0 ? sb.Length + 1 : 0;
                headings.Add(new BookmarkItem(headingText, position));
            }
        }

        // Depth-first traversal
        if (node.HasChildNodes)
        {
            foreach (var child in node.ChildNodes)
            {
                ExtractNodeText(child, sb, headings);
            }
        }
        else if (node.NodeType == HtmlNodeType.Text)
        {
            string text = WebUtility.HtmlDecode(node.InnerText);
            if (!string.IsNullOrWhiteSpace(text))
            {
                if (sb.Length > 0 && !char.IsWhiteSpace(sb[^1]))
                {
                    sb.Append(' ');
                }
                sb.Append(text.Trim());
            }
        }

        // Add spacing after block-level elements
        if (name is "p" or "div" or "h1" or "h2" or "h3" or "h4" or "h5" or "h6" or "li" or "blockquote" or "tr" or "br")
        {
            if (sb.Length > 0 && !char.IsWhiteSpace(sb[^1]))
            {
                sb.Append(' ');
            }
        }
    }
}
