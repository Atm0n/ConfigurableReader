using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Markup.Xaml.Styling;

namespace ConfigurableReader.Services;

public static class LocalizationService
{
    public static string GetString(string key)
    {
        if (Application.Current?.Resources.TryGetResource(key, Application.Current.ActualThemeVariant, out var resource) == true && resource is string str)
        {
            return str;
        }
        return key; // Fallback to key if not found
    }

    public static void SetLanguage(string languageCode)
    {
        if (Application.Current == null) return;

        // Find the existing localization dictionary if any
        var translations = Application.Current.Resources.MergedDictionaries
            .OfType<ResourceInclude>()
            .FirstOrDefault(x => x.Source?.OriginalString.Contains("/Localization/") == true);

        if (translations != null)
        {
            Application.Current.Resources.MergedDictionaries.Remove(translations);
        }

        var newSource = new Uri($"avares://ConfigurableReader/Localization/{languageCode}.axaml");
        
        // Fix: In Avalonia, the constructor sets the BaseUri. 
        // We MUST set the Source property explicitly for it to load.
        var resourceInclude = new ResourceInclude((Uri?)null)
        {
            Source = newSource
        };

        Application.Current.Resources.MergedDictionaries.Add(resourceInclude);
    }
}
