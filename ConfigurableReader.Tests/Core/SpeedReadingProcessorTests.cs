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
        var segments = SpeedReadingProcessor.ProcessText(text);

        // Assert
        // "Reader" has 6 letters. At 0.5 ratio, bold length should be 3: "Rea" (Bold) + "der" (Normal)
        segments.Count.ShouldBe(2);
        
        segments[0].Text.ShouldBe("Rea");
        segments[0].IsBold.ShouldBeTrue();

        segments[1].Text.ShouldBe("der");
        segments[1].IsBold.ShouldBeFalse();
    }

    [Fact]
    public void ProcessText_WithCustomRatio_BoldsCorrectPercentage()
    {
        // Arrange
        string text = "Configurable"; // 12 letters
        double ratio = 0.25; // 12 * 0.25 = 3 letters bolded

        // Act
        var segments = SpeedReadingProcessor.ProcessText(text, ratio);

        // Assert
        segments.Count.ShouldBe(2);
        
        segments[0].Text.ShouldBe("Con");
        segments[0].IsBold.ShouldBeTrue();

        segments[1].Text.ShouldBe("figurable");
        segments[1].IsBold.ShouldBeFalse();
    }

    [Fact]
    public void ProcessText_WithSingleLetterWord_ClampsToAtLeastOneBoldLetter()
    {
        // Arrange
        string text = "a";
        double ratio = 0.1;

        // Act
        var segments = SpeedReadingProcessor.ProcessText(text, ratio);

        // Assert
        // Even with a very small ratio, we should clamp to 1 bold letter
        segments.Count.ShouldBe(1);
        segments[0].Text.ShouldBe("a");
        segments[0].IsBold.ShouldBeTrue();
    }

    [Fact]
    public void ProcessText_WithNonWordCharacters_PreservesThemAsNormal()
    {
        // Arrange
        string text = "Hello, world!";

        // Act
        var segments = SpeedReadingProcessor.ProcessText(text);

        // Assert
        // "Hello" (Bold "Hel" + Normal "lo")
        // ", " (Normal ", ")
        // "world" (Bold "wor" + Normal "ld")
        // "!" (Normal "!")
        segments.Count.ShouldBe(6);

        segments[0].Text.ShouldBe("Hel");
        segments[0].IsBold.ShouldBeTrue();

        segments[1].Text.ShouldBe("lo");
        segments[1].IsBold.ShouldBeFalse();

        segments[2].Text.ShouldBe(", ");
        segments[2].IsBold.ShouldBeFalse();

        segments[3].Text.ShouldBe("wor");
        segments[3].IsBold.ShouldBeTrue();

        segments[4].Text.ShouldBe("ld");
        segments[4].IsBold.ShouldBeFalse();

        segments[5].Text.ShouldBe("!");
        segments[5].IsBold.ShouldBeFalse();
    }

    [Fact]
    public void GetBoldSpans_ReturnsAccurateIndexAndLengthForTextLayout()
    {
        // Arrange
        string text = "Speed Reader";

        // Act
        var spans = SpeedReadingProcessor.GetBoldSpans(text, 0.5);

        // Assert
        spans.Count.ShouldBe(2);
        // "Speed" starts at 0, 5 * 0.5 = ceil(2.5) = 3 chars bold ("Spe")
        spans[0].Index.ShouldBe(0);
        spans[0].Length.ShouldBe(3);

        // "Reader" starts at 6, 6 * 0.5 = 3 chars bold ("Rea")
        spans[1].Index.ShouldBe(6);
        spans[1].Length.ShouldBe(3);
    }
}
