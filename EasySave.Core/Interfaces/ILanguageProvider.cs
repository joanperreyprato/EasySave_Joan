namespace EasySave.Core.Interfaces;

/// <summary>
/// Provides localized string lookup and language switching for EasySave.
/// </summary>
public interface ILanguageProvider
{
    /// <summary>Gets the currently active language code (e.g. "en" or "fr").</summary>
    string CurrentLanguage { get; }

    /// <summary>
    /// Returns the localized string for the given key.
    /// Falls back to the English value if the key is missing in the active language.
    /// </summary>
    /// <param name="key">The translation key.</param>
    /// <returns>The translated string, or the key itself if no translation is found.</returns>
    string Get(string key);

    /// <summary>
    /// Changes the active language and persists the choice.
    /// </summary>
    /// <param name="languageCode">The language code to activate ("en" or "fr").</param>
    void SetLanguage(string languageCode);
}
