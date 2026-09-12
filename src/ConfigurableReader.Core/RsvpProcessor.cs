using System;
using System.Collections.Generic;

namespace ConfigurableReader.Core;

public record RsvpWord(
    string RawWord,
    string Prefix,
    char OrpChar,
    string Suffix,
    int OrpIndex,
    int StartPosition,
    int Length,
    double DelayMultiplier
);

public static class RsvpProcessor
{
    /// <summary>
    /// Calculates the Optimal Recognition Point (ORP) index within a word.
    /// Based on human eye fixation research (Spritz model).
    /// </summary>
    public static int GetOrpIndex(string word)
    {
        if (string.IsNullOrEmpty(word)) return 0;
        int len = word.Length;
        return len switch
        {
            <= 1 => 0,
            <= 5 => 1,
            <= 9 => 2,
            <= 13 => 3,
            _ => 4
        };
    }

    /// <summary>
    /// Calculates duration multiplier based on punctuation and word complexity.
    /// Gives the brain natural pauses at sentence and clause boundaries.
    /// </summary>
    public static double GetDelayMultiplier(string word)
    {
        if (string.IsNullOrEmpty(word)) return 1.0;

        char last = word[^1];
        if (last is '.' or '?' or '!' or '…')
        {
            return 2.0;
        }

        if (last is ',' or ';' or ':' or '—' or '-')
        {
            return 1.4;
        }

        if (word.Length >= 12)
        {
            return 1.2;
        }

        return 1.0;
    }

    /// <summary>
    /// Calculates the duration in milliseconds to display a word at the specified WPM.
    /// </summary>
    public static double CalculateDelayMs(double wpm, double multiplier = 1.0)
    {
        double clampedWpm = Math.Clamp(wpm, 50, 2000);
        double baseMs = 60000.0 / clampedWpm;
        return baseMs * multiplier;
    }

    /// <summary>
    /// Parses a single word token into its prefix, ORP focal character, suffix, and delay multiplier.
    /// </summary>
    public static RsvpWord ParseWord(string word, int startPosition = 0)
    {
        if (string.IsNullOrEmpty(word))
        {
            return new RsvpWord(string.Empty, string.Empty, ' ', string.Empty, 0, startPosition, 0, 1.0);
        }

        int orpIndex = GetOrpIndex(word);
        string prefix = word[..orpIndex];
        char orpChar = word[orpIndex];
        string suffix = word[(orpIndex + 1)..];
        double multiplier = GetDelayMultiplier(word);

        return new RsvpWord(word, prefix, orpChar, suffix, orpIndex, startPosition, word.Length, multiplier);
    }

    /// <summary>
    /// Tokenizes a chunk of text into structured RSVP words with character offset positions.
    /// </summary>
    public static IReadOnlyList<RsvpWord> Tokenize(string text, int basePosition = 0)
    {
        if (string.IsNullOrEmpty(text))
        {
            return [];
        }

        var words = new List<RsvpWord>();
        int i = 0;

        while (i < text.Length)
        {
            // Skip leading whitespace
            while (i < text.Length && char.IsWhiteSpace(text[i]))
            {
                i++;
            }

            if (i >= text.Length) break;

            int wordStart = i;
            while (i < text.Length && !char.IsWhiteSpace(text[i]))
            {
                i++;
            }

            string token = text[wordStart..i];
            words.Add(ParseWord(token, basePosition + wordStart));
        }

        return words;
    }

    /// <summary>
    /// Finds the word located at or immediately following <paramref name="targetPos"/> within the provided text buffer.
    /// Returns null if <paramref name="targetPos"/> exceeds the buffer or no word is found.
    /// </summary>
    public static RsvpWord? FindWordAtOrAfter(string text, int bufferStartPos, int targetPos)
    {
        if (string.IsNullOrEmpty(text)) return null;

        int relPos = targetPos - bufferStartPos;
        if (relPos < 0) relPos = 0;
        if (relPos >= text.Length) return null;

        int start = relPos;
        if (char.IsWhiteSpace(text[start]))
        {
            // Advance past whitespace to find the start of the next word
            while (start < text.Length && char.IsWhiteSpace(text[start]))
            {
                start++;
            }
            if (start >= text.Length) return null;
        }
        else
        {
            // Inside a word: rewind to the start of this word
            while (start > 0 && !char.IsWhiteSpace(text[start - 1]))
            {
                start--;
            }
        }

        int end = start;
        while (end < text.Length && !char.IsWhiteSpace(text[end]))
        {
            end++;
        }

        string word = text[start..end];
        return ParseWord(word, bufferStartPos + start);
    }

    /// <summary>
    /// Finds the word preceding the word starting at <paramref name="currentWordStartPos"/> within the buffer.
    /// Returns null if at or before the start of the buffer.
    /// </summary>
    public static RsvpWord? FindPreviousWord(string text, int bufferStartPos, int currentWordStartPos)
    {
        if (string.IsNullOrEmpty(text)) return null;

        int relPos = currentWordStartPos - bufferStartPos;
        if (relPos <= 0) return null;

        int end = relPos - 1;
        // Skip trailing whitespace before previous word
        while (end >= 0 && char.IsWhiteSpace(text[end]))
        {
            end--;
        }
        if (end < 0) return null;

        // Find start of this previous word
        int start = end;
        while (start > 0 && !char.IsWhiteSpace(text[start - 1]))
        {
            start--;
        }

        string word = text[start..(end + 1)];
        return ParseWord(word, bufferStartPos + start);
    }
}
