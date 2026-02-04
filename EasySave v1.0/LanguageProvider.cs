using System.Text.Json;

namespace EasySave_v1._0
{
    public class LanguageProvider : ILanguageProvider
    {
        private string currentLanguage;
        private Dictionary<string, Dictionary<string, string>> translations;

        // Ajout d'une instance statique pour JsonSerializerOptions pour corriger CA1869
        private static readonly JsonSerializerOptions CachedJsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        public LanguageProvider(string language)
        {
            translations = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            currentLanguage = string.IsNullOrWhiteSpace(language) ? "en" : language;
            LoadTranslations();
        }

        public string GetText(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return string.Empty;

            if (translations.TryGetValue(currentLanguage, out var dict) && dict.TryGetValue(key, out var value))
                return value;

            // fallback to english
            if (translations.TryGetValue("en", out var eng) && eng.TryGetValue(key, out var engVal))
                return engVal;

            return key;
        }

        public void SetLanguage(string languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode)) return;
            currentLanguage = languageCode;
        }

        private void LoadTranslations()
        {
            // Try to load translations from a file named "languages.json" at the application folder.
            // If it does not exist or fails, fall back to built-in defaults.
            try
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), "languages.json");
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    // Utilisation de l'instance statique pour JsonSerializerOptions
                    var data = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json, CachedJsonOptions);
                    if (data != null)
                    {
                        translations = data;
                        return;
                    }
                }
            }
            catch
            {
                // ignore and use defaults
            }

            // Default translations (minimal, extend as needed)
            translations.Clear();

            translations["en"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["menu.create"] = "Create Backup",
                ["menu.execute"] = "Execute Backup",
                ["menu.list"] = "List Backups",
                ["menu.exit"] = "Exit",
                ["error.notfound"] = "Not found"
            };

            translations["fr"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["menu.create"] = "Créer une sauvegarde",
                ["menu.execute"] = "Exécuter la sauvegarde",
                ["menu.list"] = "Lister les sauvegardes",
                ["menu.exit"] = "Quitter",
                ["error.notfound"] = "Introuvable"
            };
        }
    }
}
