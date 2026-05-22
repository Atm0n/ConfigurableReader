using System.Linq;
using ConfigurableReader.Core;
using FluentAssertions;
using Avalonia.Media;

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
        runs.Should().HaveCount(2);
        
        runs[0].Text.Should().Be("Rea");
        runs[0].FontWeight.Should().Be(FontWeight.Bold);

        runs[1].Text.Should().Be("der");
        runs[1].FontWeight.Should().Be(FontWeight.Normal);
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
        runs.Should().HaveCount(2);
        
        runs[0].Text.Should().Be("Con");
        runs[0].FontWeight.Should().Be(FontWeight.Bold);

        runs[1].Text.Should().Be("figurable");
        runs[1].FontWeight.Should().Be(FontWeight.Normal);
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
        runs.Should().HaveCount(1);
        runs[0].Text.Should().Be("a");
        runs[0].FontWeight.Should().Be(FontWeight.Bold);
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
        runs.Should().HaveCount(6);

        runs[0].Text.Should().Be("Hel");
        runs[0].FontWeight.Should().Be(FontWeight.Bold);

        runs[1].Text.Should().Be("lo");
        runs[1].FontWeight.Should().Be(FontWeight.Normal);

        runs[2].Text.Should().Be(", ");
        runs[2].FontWeight.Should().Be(FontWeight.Normal);

        runs[3].Text.Should().Be("wor");
        runs[3].FontWeight.Should().Be(FontWeight.Bold);

        runs[4].Text.Should().Be("ld");
        runs[4].FontWeight.Should().Be(FontWeight.Normal);

        runs[5].Text.Should().Be("!");
        runs[5].FontWeight.Should().Be(FontWeight.Normal);
    }
}
