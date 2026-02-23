using System.Text.Json;

namespace EasySave.Core
{
    public class LanguageProvider
    {
        private string currentLanguage;
        private Dictionary<string, Dictionary<string, string>> translations;
        private string languageConfigPath;

        // Ajout d'une instance statique pour JsonSerializerOptions pour corriger CA1869
        private static readonly JsonSerializerOptions CachedJsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Singleton support
        private static readonly object sync = new();
        private static LanguageProvider? instance;

        // Private constructor for singleton
        private LanguageProvider(string? language)
        {
            translations = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

            // Initialize language config path
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            string dir = Path.Combine(appData, "EasySave");
            Directory.CreateDirectory(dir);
            languageConfigPath = Path.Combine(dir, "language.json");

            // Try to load saved language, otherwise use provided language
            string savedLanguage = LoadLanguageFromFile();
            currentLanguage = string.IsNullOrWhiteSpace(savedLanguage) ?
                (string.IsNullOrWhiteSpace(language) ? "en" : language!) :
                savedLanguage;

            LoadTranslations();
        }

        // Get or create the singleton instance. If language is provided on first call it will be used.
        public static LanguageProvider GetInstance(string? language = null)
        {
            lock (sync)
            {
                if (instance == null)
                {
                    instance = new LanguageProvider(language);
                }
                else if (!string.IsNullOrWhiteSpace(language))
                {
                    instance.SetLanguage(language!);
                }

                return instance;
            }
        }

        // Convenience property
        public static LanguageProvider Instance => GetInstance();

        public string CurrentLanguage => currentLanguage;

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
            SaveLanguageToFile(languageCode);
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
                ["menu.language"] = "Change Language",
                ["menu.exit"] = "Exit",
                ["error.notfound"] = "Not found",
                ["language.select"] = "Select language: ",
                ["language.english"] = "English",
                ["language.french"] = "Français",
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
                ["backup.create.retry.prompt"] = "\nDo you want to retry? (yes/no): ",
                ["backup.modify.empty"] = " (leave empty to keep old value): ",
                ["backup.create.log.prompt"] = "Log format (json/xml): ",
                ["ui.select.option"] = "Select an option: ",
                ["ui.backup.title"] = "-- Create Backup Job --",
                ["ui.execute.title"] = "-- Execute Backup Job --",
                ["ui.execute.prompt"] = "Enter job index to execute: ",
                ["ui.no.backups"] = "No backup jobs configured.",
                ["ui.backup.created"] = "Backup job created.",
                ["ui.backup.failed"] = "Failed to create backup job (duplicate name?).",
                ["ui.execution.requested"] = "Execution requested.",
                ["ui.invalid.index"] = "Invalid index.",
                ["menu.edit"] = "Edit Backup",
                ["ui.backup.updated"] = "Backup job updated."
                ,
                // UI elements
                ["window.title"] = "EasySave 2.0",
                ["header.subtitle"] = "Smart and secure backups",
                ["header.badge.fast"] = "Fast backups",
                ["label.language"] = "Language",
                ["tab.jobs"] = "Jobs",
                ["jobs.section.title"] = "Backup dashboard",
                ["jobs.section.desc"] = "Manage, start, and monitor your backup jobs in real time.",
                ["security.section.title"] = "Security & encryption",
                ["security.section.desc"] = "Protect your backups with extensions and a custom key.",
                ["encryption.extensions.title"] = "Extensions to encrypt",
                ["col.name"] = "Name",
                ["col.source"] = "Source",
                ["col.target"] = "Target",
                ["col.state"] = "State",
                ["btn.add"] = "Add job",
                ["btn.startall"] = "Start all",
                ["btn.stopall"] = "Stop all",
                ["btn.startsel"] = "Start selected",
                ["btn.stopsel"] = "Stop selected",
                ["status.ready"] = "Ready",
                ["addjob.title"] = "Add Backup Job",
                ["addjob.description"] = "Fill in the backup information and log format.",
                ["addjob.name"] = "Name:",
                ["addjob.source"] = "Source:",
                ["addjob.target"] = "Destination:",
                ["addjob.log"] = "Log:",
                ["button.ok"] = "Ok",
                ["button.cancel"] = "Cancel"
                ,
                // Encryption tab texts
                ["encryption.code.label"] = "Encryption key",
                ["encryption.description"] = "Enter a key here. When starting jobs you will be asked whether to encrypt or not.",
                ["tab.encryption"] = "Encryption",
                ["encryption.prompt.encrypt"] = "Encrypt files after copy?",
                ["encryption.key.missing"] = "No encryption key provided. Encryption will be skipped.",
                ["encryption.prompt.title"] = "Encryption"
                ,
                ["encryption.add"] = "Add",
                ["encryption.remove"] = "Remove",
                ["encryption.list.desc"] = "List of extensions that will be encrypted."
                ,
                // Business software monitoring
                ["business.error.title"] = "Business Software Running",
                ["business.error.message"] = "Cannot start backup: the following business software is running:",
                ["business.error.instruction"] = "Please close these applications before starting the backup."
            };

            translations["fr"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["menu.create"] = "Créer une sauvegarde",
                ["menu.execute"] = "Exécuter la sauvegarde",
                ["menu.list"] = "Lister les sauvegardes",
                ["menu.language"] = "Changer la langue",
                ["menu.exit"] = "Quitter",
                ["error.notfound"] = "Introuvable",
                ["language.select"] = "Sélectionnez la langue : ",
                ["language.english"] = "English",
                ["language.french"] = "Français",
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
                ["backup.create.retry.prompt"] = "\nVoulez-vous réessayer ? (oui/non) : ",
                ["backup.modify.empty"] = " (laissez vide pour garder l'ancienne valeure): ",
                ["ui.select.option"] = "Sélectionnez une option : ",
                ["ui.backup.title"] = "-- Créer une sauvegarde --",
                ["ui.execute.title"] = "-- Exécuter une sauvegarde --",
                ["ui.execute.prompt"] = "Entrez l'index de la sauvegarde à exécuter : ",
                ["ui.no.backups"] = "Aucune sauvegarde configurée.",
                ["ui.backup.created"] = "Sauvegarde créée.",
                ["ui.backup.failed"] = "Impossible de créer la sauvegarde (nom déjà existant?).",
                ["ui.execution.requested"] = "Exécution lancée.",
                ["ui.invalid.index"] = "Index invalide.",
                ["menu.edit"] = "Modifier une sauvegarde",
                ["ui.backup.updated"] = "Sauvegarde mise à jour."
                ,
                // UI elements
                ["window.title"] = "EasySave 2.0",
                ["header.subtitle"] = "Sauvegardes intelligentes et securisees",
                ["header.badge.fast"] = "Sauvegardes rapides",
                ["label.language"] = "Langue",
                ["tab.jobs"] = "Travaux",
                ["jobs.section.title"] = "Tableau des sauvegardes",
                ["jobs.section.desc"] = "Gerez, demarrez et surveillez vos travaux en temps reel.",
                ["security.section.title"] = "Securite et chiffrement",
                ["security.section.desc"] = "Protegez vos sauvegardes avec des extensions et un code personnalise.",
                ["encryption.extensions.title"] = "Extensions a chiffrer",
                ["col.name"] = "Nom",
                ["col.source"] = "Source",
                ["col.target"] = "Cible",
                ["col.state"] = "Etat",
                ["btn.add"] = "Ajouter un travail",
                ["btn.startall"] = "Lancer tous",
                ["btn.stopall"] = "Arreter tous",
                ["btn.startsel"] = "Lancer sélection",
                ["btn.stopsel"] = "Arrêter sélection",
                ["status.ready"] = "Prêt",
                ["addjob.title"] = "Ajouter un travail",
                ["addjob.description"] = "Renseignez les informations de sauvegarde et le format de log.",
                ["addjob.name"] = "Nom:",
                ["addjob.source"] = "Source:",
                ["addjob.target"] = "Cible:",
                ["addjob.log"] = "Log:",
                ["button.ok"] = "Ok",
                ["button.cancel"] = "Annuler"
                ,
                // Textes pour l'onglet chiffrement
                ["encryption.code.label"] = "Code de chiffrement",
                ["encryption.description"] = "Entrez un code ici. Lors du lancement des travaux, vous serez invité à chiffrer ou non.",
                ["tab.encryption"] = "Chiffrement",
                ["encryption.prompt.encrypt"] = "Voulez-vous chiffrer les fichiers après copie ?",
                ["encryption.key.missing"] = "Aucun code de chiffrement fourni. Le chiffrement sera ignoré.",
                ["encryption.prompt.title"] = "Chiffrement"
                ,
                ["encryption.add"] = "Ajouter",
                ["encryption.remove"] = "Supprimer",
                ["encryption.list.desc"] = "Liste des extensions qui seront chiffrées."
                ,
                // Business software monitoring
                ["business.error.title"] = "Logiciel métier en cours d'exécution",
                ["business.error.message"] = "Impossible de démarrer la sauvegarde : le(s) logiciel(s) métier(s) suivant(s) est/sont en cours d'exécution :",
                ["business.error.instruction"] = "Veuillez fermer ces applications avant de lancer la sauvegarde."
            };
        }

        private void SaveLanguageToFile(string languageCode)
        {
            try
            {
                var config = new { language = languageCode };
                string json = JsonSerializer.Serialize(config, CachedJsonOptions);
                File.WriteAllText(languageConfigPath, json);
            }
            catch
            {
                // Ignore errors when saving language preference
            }
        }

        private string LoadLanguageFromFile()
        {
            try
            {
                if (!File.Exists(languageConfigPath))
                    return string.Empty;

                string json = File.ReadAllText(languageConfigPath);
                using JsonDocument doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("language", out JsonElement langElement))
                {
                    return langElement.GetString() ?? string.Empty;
                }
            }
            catch
            {
                // Ignore errors when loading language preference
            }

            return string.Empty;
        }
    }
}
