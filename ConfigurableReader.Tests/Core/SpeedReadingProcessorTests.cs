using System.Linq;
using Avalonia.Media;
using ConfigurableReader.Core;
using Shouldly;

namespace ConfigurableReader.Tests.Core;

public class SpeedReadingProcessorTests
{
    [Fact]
    public void ProcessText_WithDefaultRatio_BoldsHalfOfWord()
    {
        // Arrange
        string text = "Reader";

        // Act
        var runs = SpeedReadingProcessor.ProcessText(text);

        // Assert
        // "Reader" has 6 letters. At 0.5 ratio, bold length should be 3: "Rea" (Bold) + "der" (Normal)
        runs.Count.ShouldBe(2);
        
        runs[0].Text.ShouldBe("Rea");
        runs[0].FontWeight.ShouldBe(FontWeight.Bold);

        runs[1].Text.ShouldBe("der");
        runs[1].FontWeight.ShouldBe(FontWeight.Normal);
    }

    [Fact]
    public void ProcessText_WithCustomRatio_BoldsCorrectPercentage()
    {
        // Arrange
        string text = "Configurable"; // 12 letters
        double ratio = 0.25; // 12 * 0.25 = 3 letters bolded

        // Act
        var runs = SpeedReadingProcessor.ProcessText(text, ratio);

        // Assert
        runs.Count.ShouldBe(2);
        
        runs[0].Text.ShouldBe("Con");
        runs[0].FontWeight.ShouldBe(FontWeight.Bold);

        runs[1].Text.ShouldBe("figurable");
        runs[1].FontWeight.ShouldBe(FontWeight.Normal);
    }

    [Fact]
    public void ProcessText_WithSingleLetterWord_ClampsToAtLeastOneBoldLetter()
    {
        // Arrange
        string text = "a";
        double ratio = 0.1;

        // Act
        var runs = SpeedReadingProcessor.ProcessText(text, ratio);

        // Assert
        // Even with a very small ratio, we should clamp to 1 bold letter
        runs.Count.ShouldBe(1);
        runs[0].Text.ShouldBe("a");
        runs[0].FontWeight.ShouldBe(FontWeight.Bold);
    }

    [Fact]
    public void ProcessText_WithNonWordCharacters_PreservesThemAsNormal()
    {
        // Arrange
        string text = "Hello, world!";

        // Act
        var runs = SpeedReadingProcessor.ProcessText(text);

        // Assert
        // "Hello" (Bold "Hel" + Normal "lo")
        // ", " (Normal ", ")
        // "world" (Bold "wor" + Normal "ld")
        // "!" (Normal "!")
        runs.Count.ShouldBe(6);

        runs[0].Text.ShouldBe("Hel");
        runs[0].FontWeight.ShouldBe(FontWeight.Bold);

        runs[1].Text.ShouldBe("lo");
        runs[1].FontWeight.ShouldBe(FontWeight.Normal);

        runs[2].Text.ShouldBe(", ");
        runs[2].FontWeight.ShouldBe(FontWeight.Normal);

        runs[3].Text.ShouldBe("wor");
        runs[3].FontWeight.ShouldBe(FontWeight.Bold);

        runs[4].Text.ShouldBe("ld");
        runs[4].FontWeight.ShouldBe(FontWeight.Normal);

        runs[5].Text.ShouldBe("!");
        runs[5].FontWeight.ShouldBe(FontWeight.Normal);
    }
}
