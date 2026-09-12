using ConfigurableReader.Core;
using Shouldly;
using System.Linq;
using Xunit;

namespace ConfigurableReader.Tests;

public class RsvpProcessorTests
{
    [Theory]
    [InlineData("", 0)]
    [InlineData("a", 0)]
    [InlineData("to", 1)]
    [InlineData("the", 1)]
    [InlineData("read", 1)]
    [InlineData("speed", 1)]
    [InlineData("reader", 2)]
    [InlineData("comprehend", 3)]
    [InlineData("configuration", 3)]
    [InlineData("internationally", 4)]
    public void GetOrpIndex_ShouldReturnCorrectFocalPoint(string word, int expectedIndex)
    {
        RsvpProcessor.GetOrpIndex(word).ShouldBe(expectedIndex);
    }

    [Theory]
    [InlineData("hello", 1.0)]
    [InlineData("world.", 2.0)]
    [InlineData("question?", 2.0)]
    [InlineData("exclamation!", 2.0)]
    [InlineData("pause,", 1.4)]
    [InlineData("semicolon;", 1.4)]
    [InlineData("unbelievablewordlength", 1.2)]
    public void GetDelayMultiplier_ShouldCalculatePunctuationPauses(string word, double expectedMultiplier)
    {
        RsvpProcessor.GetDelayMultiplier(word).ShouldBe(expectedMultiplier, 0.01);
    }

    [Fact]
    public void ParseWord_ShouldSplitIntoPrefixOrpAndSuffix()
    {
        var rsvp = RsvpProcessor.ParseWord("speed", 100);

        rsvp.RawWord.ShouldBe("speed");
        rsvp.OrpIndex.ShouldBe(1);
        rsvp.Prefix.ShouldBe("s");
        rsvp.OrpChar.ShouldBe('p');
        rsvp.Suffix.ShouldBe("eed");
        rsvp.StartPosition.ShouldBe(100);
        rsvp.Length.ShouldBe(5);
    }

    [Fact]
    public void CalculateDelayMs_At300Wpm_ShouldBe200Ms()
    {
        // 60,000 / 300 = 200ms
        double delay = RsvpProcessor.CalculateDelayMs(300, 1.0);
        delay.ShouldBe(200.0, 0.01);

        // With sentence end (2.0x) -> 400ms
        double sentenceDelay = RsvpProcessor.CalculateDelayMs(300, 2.0);
        sentenceDelay.ShouldBe(400.0, 0.01);
    }

    [Fact]
    public void Tokenize_ShouldExtractWordsWithAccurateOffsets()
    {
        string text = "Speed reading is fast!";
        var words = RsvpProcessor.Tokenize(text, 50);

        words.Count.ShouldBe(4);

        words[0].RawWord.ShouldBe("Speed");
        words[0].StartPosition.ShouldBe(50);

        words[1].RawWord.ShouldBe("reading");
        words[1].StartPosition.ShouldBe(56);

        words[2].RawWord.ShouldBe("is");
        words[2].StartPosition.ShouldBe(64);

        words[3].RawWord.ShouldBe("fast!");
        words[3].StartPosition.ShouldBe(67);
        words[3].DelayMultiplier.ShouldBe(2.0);
    }

    [Fact]
    public void FindWordAtOrAfter_ShouldFindCurrentOrNextWord()
    {
        string text = "Speed reading is fast!";
        int bufferStart = 100;

        // Position at start of first word
        var w1 = RsvpProcessor.FindWordAtOrAfter(text, bufferStart, 100);
        w1.ShouldNotBeNull();
        w1.RawWord.ShouldBe("Speed");
        w1.StartPosition.ShouldBe(100);

        // Position in the middle of first word
        var w1Mid = RsvpProcessor.FindWordAtOrAfter(text, bufferStart, 102);
        w1Mid.ShouldNotBeNull();
        w1Mid.RawWord.ShouldBe("Speed");
        w1Mid.StartPosition.ShouldBe(100);

        // Position on whitespace between "Speed" and "reading"
        var w2 = RsvpProcessor.FindWordAtOrAfter(text, bufferStart, 105);
        w2.ShouldNotBeNull();
        w2.RawWord.ShouldBe("reading");
        w2.StartPosition.ShouldBe(106);

        // Position past the end
        var wEnd = RsvpProcessor.FindWordAtOrAfter(text, bufferStart, 150);
        wEnd.ShouldBeNull();
    }

    [Fact]
    public void FindPreviousWord_ShouldFindPrecedingWord()
    {
        string text = "Speed reading is fast!";
        int bufferStart = 100;

        // From "reading" (start 106), previous should be "Speed" (start 100)
        var prev = RsvpProcessor.FindPreviousWord(text, bufferStart, 106);
        prev.ShouldNotBeNull();
        prev.RawWord.ShouldBe("Speed");
        prev.StartPosition.ShouldBe(100);

        // From "fast!" (start 117), previous should be "is" (start 114)
        var prev2 = RsvpProcessor.FindPreviousWord(text, bufferStart, 117);
        prev2.ShouldNotBeNull();
        prev2.RawWord.ShouldBe("is");
        prev2.StartPosition.ShouldBe(114);

        // From first word at start of buffer, previous should be null
        var prevStart = RsvpProcessor.FindPreviousWord(text, bufferStart, 100);
        prevStart.ShouldBeNull();
    }
}
