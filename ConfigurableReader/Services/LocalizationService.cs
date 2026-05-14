using System;
using Avalonia;
using Avalonia.Controls;

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
        // This is where we would implement runtime language switching
        // For now, it's just a placeholder to show the intent.
        // We would remove the old dictionary and add the new one to Application.Current.Resources.MergedDictionaries
    }
}
