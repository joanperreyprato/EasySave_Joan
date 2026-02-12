using System;
using System.Collections.Generic;
using EasyLog;
using EasyLog;

namespace EasySave_v1._0
{
    public class ConsoleUI
    {
        private readonly BackupManager backupManager;
        private readonly LanguageProvider languageProvider; // use concrete LanguageProvider

        public ConsoleUI(BackupManager manager, LanguageProvider language)
        {
            this.backupManager = manager ?? throw new ArgumentNullException(nameof(manager));
            this.languageProvider = language ?? throw new ArgumentNullException(nameof(language));
        }

        public void DisplayMainMenu()
        {
            Console.WriteLine("=== EasySave ===");
            Console.WriteLine("1. " + languageProvider.GetText("menu.create"));
            Console.WriteLine("2. " + languageProvider.GetText("menu.execute"));
            Console.WriteLine("3. " + languageProvider.GetText("menu.list"));
            Console.WriteLine("4. " + languageProvider.GetText("menu.edit"));
            Console.WriteLine("5. " + languageProvider.GetText("menu.language"));
            Console.WriteLine("6. " + languageProvider.GetText("menu.exit"));
            Console.Write(languageProvider.GetText("ui.select.option"));
        }

        public void ChangeLanguageMenu()
        {
            Console.WriteLine("-- " + languageProvider.GetText("menu.language") + " --");
            Console.WriteLine("1. " + languageProvider.GetText("language.english"));
            Console.WriteLine("2. " + languageProvider.GetText("language.french"));
            Console.Write(languageProvider.GetText("language.select"));
        }

        public void CreateBackupJobMenu()
        {
            Console.WriteLine(languageProvider.GetText("ui.backup.title"));
            string name = GetUserInput(languageProvider.GetText("backup.create.name.prompt"));
            string source = GetUserInput(languageProvider.GetText("backup.create.source.prompt"));
            string target = GetUserInput(languageProvider.GetText("backup.create.target.prompt"));
            string typeStr = GetUserInput(languageProvider.GetText("backup.create.type.prompt"));

            // Prompt for log format (json/xml)
            string logPrompt = languageProvider.GetText("backup.create.log.prompt");
            if (string.IsNullOrWhiteSpace(logPrompt)) logPrompt = "Log format (json/xml): ";
            string logStr = GetUserInput(logPrompt);
            EasyLog.LogFormat logFormat = EasyLog.LogFormat.Json;
            if (!string.IsNullOrWhiteSpace(logStr) && logStr.Trim().StartsWith("x", StringComparison.OrdinalIgnoreCase))
                logFormat = EasyLog.LogFormat.Xml;

            BackupType type = BackupType.Full;
            if (!string.IsNullOrWhiteSpace(typeStr) && typeStr.StartsWith("d", StringComparison.OrdinalIgnoreCase))
                type = BackupType.Differential;

            bool ok = backupManager.CreateBackupJob(name, source, target, type, logFormat, out var error);
            if (ok)
                Console.WriteLine(languageProvider.GetText("ui.backup.created"));
            else
            {
                Console.WriteLine(languageProvider.GetText("backup.create.error.title"));
                Console.WriteLine(GetErrorMessage(error));
            }
        }

        public void ExecuteBackupMenu()
        {
            Console.WriteLine(languageProvider.GetText("ui.execute.title"));
            DisplayBackupJobs();
            string idxStr = GetUserInput(languageProvider.GetText("ui.execute.prompt"));
            if (int.TryParse(idxStr, out int idx))
            {
                backupManager.ExecuteBackupJob(idx);
                Console.WriteLine(languageProvider.GetText("ui.execution.requested"));
            }
            else
            {
                Console.WriteLine(languageProvider.GetText("ui.invalid.index"));
            }
        }

        public void EditBackupJobMenu()
        {
            Console.WriteLine("-- " + languageProvider.GetText("menu.edit") + " --");
            DisplayBackupJobs();
            string idxStr = GetUserInput(languageProvider.GetText("ui.execute.prompt"));
            if (!int.TryParse(idxStr, out int idx))
            {
                Console.WriteLine(languageProvider.GetText("ui.invalid.index"));
                return;
            }

            // Ask for new values; empty input keeps existing
            string name = GetUserInput(languageProvider.GetText("backup.create.name.prompt") + languageProvider.GetText("backup.modify.empty"));
            string source = GetUserInput(languageProvider.GetText("backup.create.source.prompt") + languageProvider.GetText("backup.modify.empty"));
            string target = GetUserInput(languageProvider.GetText("backup.create.target.prompt") + languageProvider.GetText("backup.modify.empty"));
            string typeStr = GetUserInput(languageProvider.GetText("backup.create.type.prompt") + languageProvider.GetText("backup.modify.empty"));

            BackupType type = BackupType.Full;
            if (!string.IsNullOrWhiteSpace(typeStr) && typeStr.StartsWith("d", StringComparison.OrdinalIgnoreCase))
                type = BackupType.Differential;

            if (backupManager.UpdateBackupJob(idx, string.IsNullOrWhiteSpace(name) ? null : name, string.IsNullOrWhiteSpace(source) ? null : source, string.IsNullOrWhiteSpace(target) ? null : target, type, out var error))
            {
                Console.WriteLine(languageProvider.GetText("ui.backup.updated"));
            }
            else
            {
                Console.WriteLine(languageProvider.GetText("backup.create.error.title"));
                Console.WriteLine(GetErrorMessage(error));
            }
        }

        private string GetErrorMessage(BackupManager.BackupCreationError error)
        {
            return error switch
            {
                BackupManager.BackupCreationError.InvalidName => languageProvider.GetText("backup.create.error.invalid_name"),
                BackupManager.BackupCreationError.SourcePathInvalid => languageProvider.GetText("backup.create.error.source_path_invalid"),
                BackupManager.BackupCreationError.SourcePathNotFound => languageProvider.GetText("backup.create.error.source_path_not_found"),
                BackupManager.BackupCreationError.TargetPathInvalid => languageProvider.GetText("backup.create.error.target_path_invalid"),
                BackupManager.BackupCreationError.TargetPathNotFound => languageProvider.GetText("backup.create.error.target_path_not_found"),
                BackupManager.BackupCreationError.DuplicateName => languageProvider.GetText("backup.create.error.duplicate_name"),
                BackupManager.BackupCreationError.MaxJobsExceeded => languageProvider.GetText("backup.create.error.max_jobs_exceeded"),
                _ => languageProvider.GetText("backup.create.error.unknown")
            };
        }

        public void DisplayBackupJobs()
        {
            List<BackupJob> jobs = backupManager.GetBackupJobs();
            if (jobs == null || jobs.Count == 0)
            {
                Console.WriteLine(languageProvider.GetText("ui.no.backups"));
                return;
            }

            Console.WriteLine();

            for (int i = 0; i < jobs.Count; i++)
            {
                BackupJob j = jobs[i];
                Console.WriteLine($"[{i+1}] {j.GetName()} -> {j.GetSourcePath()} => {j.GetTargetPath()} ({j.GetBackupType()})\n");
                
            }
        }

        public static string GetUserInput(string prompt)
        {
            Console.Write(prompt);
            return Console.ReadLine() ?? string.Empty;
        }
    }
}
