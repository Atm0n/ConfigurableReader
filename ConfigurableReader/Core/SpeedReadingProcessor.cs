using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace ConfigurableReader.Core;

public static class SpeedReadingProcessor
{
    private static readonly Regex WordRegex = new Regex(@"(\p{L}+)|([^\p{L}]+)", RegexOptions.Compiled);

    public static List<Run> ProcessText(string text, double boldRatio = 0.5)
    {
        var runs = new List<Run>();
        var matches = WordRegex.Matches(text);

        foreach (Match match in matches)
        {
            if (match.Groups[1].Success) // It's a word
            {
                string word = match.Value;
                int boldLength = Math.Clamp((int)Math.Ceiling(word.Length * boldRatio), 1, word.Length);
                
                string boldPart = word.Substring(0, boldLength);
                string normalPart = word.Substring(boldLength);

                runs.Add(new Run(boldPart) { FontWeight = FontWeight.Bold });
                
                if (normalPart.Length > 0)
                {
                    runs.Add(new Run(normalPart) { FontWeight = FontWeight.Normal });
                }
            }
            else // It's whitespace or punctuation
            {
                runs.Add(new Run(match.Value) { FontWeight = FontWeight.Normal });
            }
        }

        return runs;
    }
}
