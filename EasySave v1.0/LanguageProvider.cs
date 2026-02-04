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

            if (translations.TryGetValue(currentLanguage, out Dictionary<string, string>? dict) && dict.TryGetValue(key, out string? value))
                return value;

            // fallback to english
            if (translations.TryGetValue("en", out Dictionary<string, string>? eng) && eng.TryGetValue(key, out string? engVal))
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
                string path = Path.Combine(Directory.GetCurrentDirectory(), "languages.json");
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    // Utilisation de l'instance statique pour JsonSerializerOptions
                    Dictionary<string, Dictionary<string, string>>? data = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json, CachedJsonOptions);
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
                ["error.notfound"] = "Not found",
                ["backup.create.title"] = "-- Create Backup Job --",
                ["backup.create.name.prompt"] = "Name: ",
                ["backup.create.source.prompt"] = "Source path: ",
                ["backup.create.target.prompt"] = "Target path: ",
                ["backup.create.type.prompt"] = "Type (full/diff): ",
                ["backup.create.success"] = "Backup job created successfully.",
                ["backup.create.error.title"] = "[ERROR] Failed to create backup job:",
                ["backup.create.error.invalid_name"] = "  - Invalid job name. Use only alphanumeric characters, spaces, hyphens, and underscores (max 50 characters).",
                ["backup.create.error.source_path_invalid"] = "  - Source path is invalid. Please check the path format.",
                ["backup.create.error.source_path_not_found"] = "  - Source directory does not exist. Please check the path.",
                ["backup.create.error.target_path_invalid"] = "  - Target path is invalid. Please check the path format.",
                ["backup.create.error.target_path_not_found"] = "  - Target directory does not exist. Please check the path.",
                ["backup.create.error.duplicate_name"] = "  - A backup job with this name already exists.",
                ["backup.create.error.max_jobs_exceeded"] = "  - Maximum number of backup jobs (5) reached.",
                ["backup.create.error.unknown"] = "  - An unknown error occurred.",
                ["backup.create.retry.prompt"] = "\nDo you want to retry? (yes/no): "
            };

            translations["fr"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["menu.create"] = "Créer une sauvegarde",
                ["menu.execute"] = "Exécuter la sauvegarde",
                ["menu.list"] = "Lister les sauvegardes",
                ["menu.exit"] = "Quitter",
                ["error.notfound"] = "Introuvable",
                ["backup.create.title"] = "-- Créer une sauvegarde --",
                ["backup.create.name.prompt"] = "Nom : ",
                ["backup.create.source.prompt"] = "Chemin source : ",
                ["backup.create.target.prompt"] = "Chemin cible : ",
                ["backup.create.type.prompt"] = "Type (complet/diff) : ",
                ["backup.create.success"] = "Sauvegarde créée avec succès.",
                ["backup.create.error.title"] = "[ERREUR] Impossible de créer la sauvegarde :",
                ["backup.create.error.invalid_name"] = "  - Nom invalide. Utilisez uniquement des caractères alphanumériques, des espaces, des tirets et des underscores (max 50 caractères).",
                ["backup.create.error.source_path_invalid"] = "  - Le chemin source est invalide. Vérifiez le format du chemin.",
                ["backup.create.error.source_path_not_found"] = "  - Le répertoire source n'existe pas. Vérifiez le chemin.",
                ["backup.create.error.target_path_invalid"] = "  - Le chemin cible est invalide. Vérifiez le format du chemin.",
                ["backup.create.error.target_path_not_found"] = "  - Le répertoire cible n'existe pas. Vérifiez le chemin.",
                ["backup.create.error.duplicate_name"] = "  - Une sauvegarde portant ce nom existe déjà.",
                ["backup.create.error.max_jobs_exceeded"] = "  - Nombre maximum de sauvegardes (5) atteint.",
                ["backup.create.error.unknown"] = "  - Une erreur inconnue s'est produite.",
                ["backup.create.retry.prompt"] = "\nVoulez-vous réessayer ? (oui/non) : "
            };
        }
    }
}
