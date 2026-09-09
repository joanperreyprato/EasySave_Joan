namespace EasySave.WPF;

/// <summary>
/// Manages the application-wide theme (Dark / Light) by swapping ResourceDictionary entries.
/// </summary>
public static class ThemeManager
{
    private const string DarkThemeUri  = "/Styles/DarkTheme.xaml";
    private const string LightThemeUri = "/Styles/LightTheme.xaml";

    /// <summary>Applies either the dark or light theme to the current application.</summary>
    public static void Apply(bool isDark)
    {
        var uri = new Uri(isDark ? DarkThemeUri : LightThemeUri, UriKind.Relative);
        var dict = new System.Windows.ResourceDictionary { Source = uri };

        var merged = System.Windows.Application.Current.Resources.MergedDictionaries;
        var existing = merged
            .Where(d => d.Source != null && d.Source.OriginalString.Contains("Theme"))
            .ToList();
        foreach (var d in existing) merged.Remove(d);
        merged.Add(dict);
    }
}
