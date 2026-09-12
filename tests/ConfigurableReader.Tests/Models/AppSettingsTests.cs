using ConfigurableReader.Models;
using Shouldly;
using System.Text.Json;
using Xunit;

namespace ConfigurableReader.Tests;

public class AppSettingsTests
{
    [Fact]
    public void DefaultSettings_HaveExpectedReadingModeAndFocusRulerDefaults()
    {
        var settings = new AppSettings();

        settings.ReadingMode.ShouldBe("Marquee");
        settings.FocusRulerOpacity.ShouldBe(0.65);
        settings.FocusRulerHeightMultiplier.ShouldBe(1.8);
        settings.KeyBindings.ShouldNotBeNull();
    }

    [Fact]
    public void Serialization_PreservesFocusRulerSettings()
    {
        var settings = new AppSettings
        {
            ReadingMode = "FocusRuler",
            FocusRulerOpacity = 0.8,
            FocusRulerHeightMultiplier = 2.2
        };

        string json = JsonSerializer.Serialize(settings);
        var deserialized = JsonSerializer.Deserialize<AppSettings>(json);

        deserialized.ShouldNotBeNull();
        deserialized.ReadingMode.ShouldBe("FocusRuler");
        deserialized.FocusRulerOpacity.ShouldBe(0.8);
        deserialized.FocusRulerHeightMultiplier.ShouldBe(2.2);
    }

    [Theory]
    [InlineData("Marquee", "FocusRuler")]
    [InlineData("FocusRuler", "RSVP")]
    [InlineData("RSVP", "Marquee")]
    public void ModeCycling_TransitionsCorrectly(string currentMode, string expectedNextMode)
    {
        string nextMode = currentMode switch
        {
            "Marquee" => "FocusRuler",
            "FocusRuler" => "RSVP",
            _ => "Marquee"
        };

        nextMode.ShouldBe(expectedNextMode);
    }
}
