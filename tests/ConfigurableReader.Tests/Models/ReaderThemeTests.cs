using System;
using System.Linq;
using ConfigurableReader.Models;
using Shouldly;
using Xunit;

namespace ConfigurableReader.Tests;

public class ReaderThemeTests
{
    [Fact]
    public void Presets_ShouldContainStandardThemes()
    {
        var presets = ReaderTheme.Presets;

        presets.ShouldNotBeNull();
        presets.Count.ShouldBeGreaterThanOrEqualTo(8);

        var keys = presets.Select(p => p.Key).ToList();
        keys.ShouldContain("System Default");
        keys.ShouldContain("Dark");
        keys.ShouldContain("Light");
        keys.ShouldContain("Sepia");
        keys.ShouldContain("OLED Black");
        keys.ShouldContain("Solarized Dark");
        keys.ShouldContain("Nord");
        keys.ShouldContain("High Contrast");
        keys.ShouldContain("Custom");
    }

    [Fact]
    public void Presets_ShouldHaveUniqueKeys()
    {
        var keys = ReaderTheme.Presets.Select(p => p.Key).ToList();
        keys.Distinct().Count().ShouldBe(keys.Count);
    }

    [Fact]
    public void Presets_ShouldHaveValidHexColors()
    {
        foreach (var theme in ReaderTheme.Presets)
        {
            theme.Background.ShouldStartWith("#");
            theme.Foreground.ShouldStartWith("#");
            theme.ControlBarBackground.ShouldStartWith("#");
            theme.ControlBarForeground.ShouldStartWith("#");
            theme.BorderBrush.ShouldStartWith("#");

            theme.LocalizationKey.ShouldNotBeNullOrWhiteSpace();
        }
    }
}
