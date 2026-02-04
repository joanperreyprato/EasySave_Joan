using System;
using System.Collections.Generic;

namespace EasySave_v1._0
{
    public class ConsoleUI
    {
        private readonly BackupManager backupManager;
        private readonly ILanguageProvider languageProvider;

        public ConsoleUI(BackupManager manager, ILanguageProvider language)
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
            Console.WriteLine("4. " + languageProvider.GetText("menu.exit"));
            Console.Write("Select an option: ");
        }

        public void CreateBackupJobMenu()
        {
            Console.WriteLine("-- Create Backup Job --");
            string name = GetUserInput("Name: ");
            string source = GetUserInput("Source path: ");
            string target = GetUserInput("Target path: ");
            string typeStr = GetUserInput("Type (full/diff): ");

            BackupType type = BackupType.Full;
            if (!string.IsNullOrWhiteSpace(typeStr) && typeStr.StartsWith("d", StringComparison.OrdinalIgnoreCase))
                type = BackupType.Differential;

            bool ok = backupManager.CreateBackupJob(name, source, target, type);
            Console.WriteLine(ok ? "Backup job created." : "Failed to create backup job (duplicate name?).");
        }

        public void ExecuteBackupMenu()
        {
            Console.WriteLine("-- Execute Backup Job --");
            DisplayBackupJobs();
            string idxStr = GetUserInput("Enter job index to execute: ");
            if (int.TryParse(idxStr, out int idx))
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
            List<BackupJob> jobs = backupManager.GetBackupJobs();
            if (jobs == null || jobs.Count == 0)
            {
                Console.WriteLine("No backup jobs configured.");
                return;
            }

            for (int i = 0; i < jobs.Count; i++)
            {
                BackupJob j = jobs[i];
                Console.WriteLine($"[{i+1}] {j.GetName()} -> {j.GetSourcePath()} => {j.GetTargetPath()} ({j.GetType()})");
            }
        }

        public static string GetUserInput(string prompt)
        {
            Console.Write(prompt);
            return Console.ReadLine() ?? string.Empty;
        }
    }
}
