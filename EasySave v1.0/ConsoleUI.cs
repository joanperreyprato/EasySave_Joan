using System;
using System.Collections.Generic;

namespace EasySave_v1._0
{
    public class ConsoleUI(BackupManager manager, ILanguageProvider language)
    {
        private readonly BackupManager backupManager = manager ?? throw new ArgumentNullException(nameof(manager));
        private readonly ILanguageProvider languageProvider = language ?? throw new ArgumentNullException(nameof(language));

        public void DisplayMainMenu()
        {
            Console.WriteLine("=== EasySave ===");
            Console.WriteLine("1. " + languageProvider.GetText("menu.create"));
            Console.WriteLine("2. " + languageProvider.GetText("menu.execute"));
            Console.WriteLine("3. " + languageProvider.GetText("menu.list"));
            Console.WriteLine("4. " + languageProvider.GetText("menu.exit"));
            Console.Write("Select an option: ");
        }

        public void CreateBackupJobMenu()
        {
            Console.WriteLine(languageProvider.GetText("backup.create.title"));
            bool jobCreated = false;

            while (!jobCreated)
            {
                var name = GetUserInput(languageProvider.GetText("backup.create.name.prompt"));
                var source = GetUserInput(languageProvider.GetText("backup.create.source.prompt"));
                var target = GetUserInput(languageProvider.GetText("backup.create.target.prompt"));
                var typeStr = GetUserInput(languageProvider.GetText("backup.create.type.prompt"));

                var type = BackupType.Full;
                if (!string.IsNullOrWhiteSpace(typeStr) && typeStr.StartsWith("d", StringComparison.OrdinalIgnoreCase))
                    type = BackupType.Differential;

                // Use the new overload with error handling
                if (backupManager.CreateBackupJob(name, source, target, type, out var error))
                {
                    Console.WriteLine(languageProvider.GetText("backup.create.success"));
                    jobCreated = true;
                }
                else
                {
                    // Display specific error message and ask user to retry
                    DisplayCreationError(error);
                    var retry = GetUserInput(languageProvider.GetText("backup.create.retry.prompt"));
                    if (!retry.StartsWith("y", StringComparison.OrdinalIgnoreCase))
                    {
                        jobCreated = true;
                    }
                }
            }
        }

        private void DisplayCreationError(BackupManager.BackupCreationError error)
        {
            Console.WriteLine(languageProvider.GetText("backup.create.error.title"));
            switch (error)
            {
                case BackupManager.BackupCreationError.InvalidName:
                    Console.WriteLine(languageProvider.GetText("backup.create.error.invalid_name"));
                    break;
                case BackupManager.BackupCreationError.SourcePathInvalid:
                    Console.WriteLine(languageProvider.GetText("backup.create.error.source_path_invalid"));
                    break;
                case BackupManager.BackupCreationError.SourcePathNotFound:
                    Console.WriteLine(languageProvider.GetText("backup.create.error.source_path_not_found"));
                    break;
                case BackupManager.BackupCreationError.TargetPathInvalid:
                    Console.WriteLine(languageProvider.GetText("backup.create.error.target_path_invalid"));
                    break;
                case BackupManager.BackupCreationError.TargetPathNotFound:
                    Console.WriteLine(languageProvider.GetText("backup.create.error.target_path_not_found"));
                    break;
                case BackupManager.BackupCreationError.DuplicateName:
                    Console.WriteLine(languageProvider.GetText("backup.create.error.duplicate_name"));
                    break;
                case BackupManager.BackupCreationError.MaxJobsExceeded:
                    Console.WriteLine(languageProvider.GetText("backup.create.error.max_jobs_exceeded"));
                    break;
                default:
                    Console.WriteLine(languageProvider.GetText("backup.create.error.unknown"));
                    break;
            }
        }

        public void ExecuteBackupMenu()
        {
            Console.WriteLine("-- Execute Backup Job --");
            DisplayBackupJobs();
            var idxStr = GetUserInput("Enter job index to execute: ");
            if (int.TryParse(idxStr, out var idx))
            {
                backupManager.ExecuteBackupJob(idx);
                Console.WriteLine("Execution requested.");
            }
            else
            {
                Console.WriteLine("Invalid index.");
            }
        }

        public void DisplayBackupJobs()
        {
            var jobs = backupManager.GetBackupJobs();
            if (jobs == null || jobs.Count == 0)
            {
                Console.WriteLine("No backup jobs configured.");
                return;
            }

            for (int i = 0; i < jobs.Count; i++)
            {
                var j = jobs[i];
                Console.WriteLine($"[{i}] {j.GetName()} -> {j.GetSourcePath()} => {j.GetTargetPath()} ({j.GetType()})");
            }
        }

        public static string GetUserInput(string prompt)
        {
            Console.Write(prompt);
            return Console.ReadLine() ?? string.Empty;
        }
    }
}
