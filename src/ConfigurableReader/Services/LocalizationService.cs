using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Markup.Xaml.Styling;
using ConfigurableReader.Common;

namespace ConfigurableReader.Services;

public static class LocalizationService
{
    // Built from AppConstants so there is one source of truth for supported languages.
    private static readonly Dictionary<string, string> TwoLetterToCode =
        AppConstants.SupportedLanguages
            .ToDictionary(
                lang => new CultureInfo(lang.Code).TwoLetterISOLanguageName,
                lang => lang.Code);

    public static string GetString(string key)
    {
        if (Application.Current?.Resources.TryGetResource(key, Application.Current.ActualThemeVariant, out var resource) == true && resource is string str)
        {
            return str;
        }
        return key;
    }

    public static void SetLanguage(string languageCode)
    {
        if (Application.Current == null) return;

        var translations = Application.Current.Resources.MergedDictionaries
            .OfType<ResourceInclude>()
            .FirstOrDefault(x => x.Source?.OriginalString.Contains("/Localization/") == true);

        if (translations != null)
        {
            Application.Current.Resources.MergedDictionaries.Remove(translations);
        }

        var newSource = new Uri($"avares://ConfigurableReader/Localization/{languageCode}.axaml");
        
        var resourceInclude = new ResourceInclude((Uri?)null)
        {
            Source = newSource
        };

        Application.Current.Resources.MergedDictionaries.Add(resourceInclude);
    }

    public static string GetSystemLanguage()
    {
        var uiCulture = CultureInfo.CurrentUICulture;
        
        // 1. Try exact match (e.g. "es-ES")
        if (AppConstants.SupportedLanguages.Any(l => l.Code == uiCulture.Name))
            return uiCulture.Name;

        // 2. Try two-letter code match (e.g. "es-MX" -> "es" -> "es-ES")
        if (TwoLetterToCode.TryGetValue(uiCulture.TwoLetterISOLanguageName, out var mappedCode))
            return mappedCode;

        return "en-US";
    }
}
