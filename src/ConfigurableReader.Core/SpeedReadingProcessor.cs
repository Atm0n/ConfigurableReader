using System.Text.RegularExpressions;

namespace ConfigurableReader.Core;

public readonly record struct SpeedReadingSegment(string Text, bool IsBold);

public static partial class SpeedReadingProcessor
{
    [GeneratedRegex(@"(\p{L}+)|([^\p{L}]+)")]
    private static partial Regex WordRegex();

    public static List<SpeedReadingSegment> ProcessText(string text, double boldRatio = 0.5)
    {
        var segments = new List<SpeedReadingSegment>();
        if (string.IsNullOrEmpty(text)) return segments;

        var matches = WordRegex().Matches(text);

        foreach (Match match in matches)
        {
            if (match.Groups[1].Success) // It's a word
            {
                string word = match.Value;
                int boldLength = Math.Clamp((int)Math.Ceiling(word.Length * boldRatio), 1, word.Length);

                string boldPart = word.Substring(0, boldLength);
                string normalPart = word.Substring(boldLength);

                segments.Add(new SpeedReadingSegment(boldPart, true));

                if (normalPart.Length > 0)
                {
                    segments.Add(new SpeedReadingSegment(normalPart, false));
                }
            }
            else // It's whitespace or punctuation
            {
                segments.Add(new SpeedReadingSegment(match.Value, false));
            }
        }

        return segments;
    }

    public static List<(int Index, int Length)> GetBoldSpans(string text, double boldRatio = 0.5)
    {
        var spans = new List<(int Index, int Length)>();
        if (string.IsNullOrEmpty(text)) return spans;

        var matches = WordRegex().Matches(text);
        foreach (Match match in matches)
        {
            if (match.Groups[1].Success)
            {
                int boldLength = Math.Clamp((int)Math.Ceiling(match.Length * boldRatio), 1, match.Length);
                spans.Add((match.Index, boldLength));
            }
        }

        return spans;
    }
}
