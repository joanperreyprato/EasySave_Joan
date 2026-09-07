using System.Text.Json;
using EasySave.Core.Interfaces;

namespace EasySave.Core.Services;

/// <summary>
/// Thread-safe singleton that provides localized strings for EasySave.
/// Supports English and French. Persists the language choice to disk.
/// </summary>
public sealed class LanguageProvider : ILanguageProvider
{
    // ── Singleton ──────────────────────────────────────────────────────────────
    private static LanguageProvider? _instance;
    private static readonly object SyncRoot = new();

    /// <summary>Gets the singleton instance, creating it on first access.</summary>
    public static LanguageProvider Instance
    {
        get
        {
            if (_instance is null)
                lock (SyncRoot)
                    _instance ??= new LanguageProvider();
            return _instance;
        }
    }

    // ── State ──────────────────────────────────────────────────────────────────
    private string _currentLanguage = "en";
    private static readonly string ConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "EasySave", "language.json");

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    // ── Translations ───────────────────────────────────────────────────────────
    private static readonly Dictionary<string, Dictionary<string, string>> Translations = new()
    {
        ["en"] = new()
        {
            ["menu_title"]              = "EasySave — Backup Manager",
            ["menu_list"]               = "List backup jobs",
            ["menu_create"]             = "Create a backup job",
            ["menu_execute"]            = "Execute backup job(s)",
            ["menu_delete"]             = "Delete a backup job",
            ["menu_quit"]               = "Quit",
            ["menu_settings"]           = "Settings",
            ["job_name"]                = "Job name",
            ["job_source"]              = "Source directory",
            ["job_target"]              = "Target directory",
            ["job_type"]                = "Backup type",
            ["job_type_full"]           = "Full",
            ["job_type_differential"]   = "Differential",
            ["error_max_jobs"]          = "Cannot create job: maximum of 5 jobs reached.",
            ["error_invalid_name"]      = "Invalid name. Use 1–50 alphanumeric characters, spaces, dashes or underscores.",
            ["error_source_not_found"]  = "Source directory not found.",
            ["error_target_not_found"]  = "Target parent directory not found.",
            ["error_duplicate_name"]    = "A job with this name already exists.",
            ["error_job_not_found"]     = "Job not found.",
            ["success_job_created"]     = "Job created successfully.",
            ["success_job_deleted"]     = "Job deleted successfully.",
            ["success_backup_complete"] = "Backup completed successfully.",
            ["prompt_enter_name"]       = "Enter job name",
            ["prompt_enter_source"]     = "Enter source directory",
            ["prompt_enter_target"]     = "Enter target directory",
            ["prompt_select_type"]      = "Select backup type",
            ["prompt_select_jobs"]      = "Select jobs to execute",
            ["status_inactive"]         = "Inactive",
            ["status_active"]           = "Active",
            ["status_completed"]        = "Completed",
            ["status_error"]            = "Error",
            ["info_no_jobs"]            = "No backup jobs configured.",
            ["info_backing_up"]         = "Running backup(s)...",
            ["info_done"]               = "Done.",
            ["settings_language"]       = "Language",
            ["settings_log_format"]     = "Log format",
            ["log_format_json"]         = "JSON",
            ["log_format_xml"]          = "XML",
        },
        ["fr"] = new()
        {
            ["menu_title"]              = "EasySave — Gestionnaire de sauvegardes",
            ["menu_list"]               = "Lister les travaux de sauvegarde",
            ["menu_create"]             = "Créer un travail de sauvegarde",
            ["menu_execute"]            = "Exécuter un ou plusieurs travaux",
            ["menu_delete"]             = "Supprimer un travail de sauvegarde",
            ["menu_quit"]               = "Quitter",
            ["menu_settings"]           = "Paramètres",
            ["job_name"]                = "Nom du travail",
            ["job_source"]              = "Répertoire source",
            ["job_target"]              = "Répertoire cible",
            ["job_type"]                = "Type de sauvegarde",
            ["job_type_full"]           = "Complète",
            ["job_type_differential"]   = "Différentielle",
            ["error_max_jobs"]          = "Impossible de créer le travail : maximum de 5 travaux atteint.",
            ["error_invalid_name"]      = "Nom invalide. Utilisez 1 à 50 caractères alphanumériques, espaces, tirets ou underscores.",
            ["error_source_not_found"]  = "Répertoire source introuvable.",
            ["error_target_not_found"]  = "Répertoire parent de la cible introuvable.",
            ["error_duplicate_name"]    = "Un travail portant ce nom existe déjà.",
            ["error_job_not_found"]     = "Travail introuvable.",
            ["success_job_created"]     = "Travail créé avec succès.",
            ["success_job_deleted"]     = "Travail supprimé avec succès.",
            ["success_backup_complete"] = "Sauvegarde terminée avec succès.",
            ["prompt_enter_name"]       = "Entrez le nom du travail",
            ["prompt_enter_source"]     = "Entrez le répertoire source",
            ["prompt_enter_target"]     = "Entrez le répertoire cible",
            ["prompt_select_type"]      = "Sélectionnez le type de sauvegarde",
            ["prompt_select_jobs"]      = "Sélectionnez les travaux à exécuter",
            ["status_inactive"]         = "Inactif",
            ["status_active"]           = "Actif",
            ["status_completed"]        = "Terminé",
            ["status_error"]            = "Erreur",
            ["info_no_jobs"]            = "Aucun travail de sauvegarde configuré.",
            ["info_backing_up"]         = "Exécution de la sauvegarde...",
            ["info_done"]               = "Terminé.",
            ["settings_language"]       = "Langue",
            ["settings_log_format"]     = "Format du journal",
            ["log_format_json"]         = "JSON",
            ["log_format_xml"]          = "XML",
        }
    };

    // ── Constructor ────────────────────────────────────────────────────────────
    private LanguageProvider() => LoadPersistedLanguage();

    // ── ILanguageProvider ──────────────────────────────────────────────────────

    /// <inheritdoc/>
    public string CurrentLanguage => _currentLanguage;

    /// <inheritdoc/>
    public string Get(string key)
    {
        if (Translations.TryGetValue(_currentLanguage, out var dict) && dict.TryGetValue(key, out var value))
            return value;

        // Fallback to English
        if (Translations.TryGetValue("en", out var en) && en.TryGetValue(key, out var fallback))
            return fallback;

        return key; // Last resort: return the key itself
    }

    /// <inheritdoc/>
    public void SetLanguage(string languageCode)
    {
        if (!Translations.ContainsKey(languageCode))
            return;

        _currentLanguage = languageCode;
        PersistLanguage();
    }

    // ── Persistence ────────────────────────────────────────────────────────────

    /// <summary>Loads the persisted language preference from disk.</summary>
    private void LoadPersistedLanguage()
    {
        try
        {
            if (!File.Exists(ConfigPath)) return;
            var json = File.ReadAllText(ConfigPath);
            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (data is not null && data.TryGetValue("language", out var lang)
                && Translations.ContainsKey(lang))
                _currentLanguage = lang;
        }
        catch { /* Ignore — fall back to default English */ }
    }

    /// <summary>Persists the current language choice to disk.</summary>
    private void PersistLanguage()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
            var json = JsonSerializer.Serialize(
                new Dictionary<string, string> { ["language"] = _currentLanguage },
                JsonOptions);
            File.WriteAllText(ConfigPath, json);
        }
        catch { /* Ignore write errors */ }
    }
}
