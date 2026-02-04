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
            Console.WriteLine("4. " + languageProvider.GetText("menu.language"));
            Console.WriteLine("5. " + languageProvider.GetText("menu.exit"));
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

            BackupType type = BackupType.Full;
            if (!string.IsNullOrWhiteSpace(typeStr) && typeStr.StartsWith("d", StringComparison.OrdinalIgnoreCase))
                type = BackupType.Differential;

            bool ok = backupManager.CreateBackupJob(name, source, target, type);
            Console.WriteLine(ok ? languageProvider.GetText("ui.backup.created") : languageProvider.GetText("ui.backup.failed"));
        }

        public void ExecuteBackupMenu()
        {
            Console.WriteLine(languageProvider.GetText("ui.execute.title"));
            DisplayBackupJobs();
            string idxStr = GetUserInput(languageProvider.GetText("ui.execute.prompt"));
            if (int.TryParse(idxStr, out int idx))
            {
                backupManager.ExecuteBackupJob(idx-1);
                Console.WriteLine(languageProvider.GetText("ui.execution.requested"));
            }
            else
            {
                Console.WriteLine(languageProvider.GetText("ui.invalid.index"));
            }
        }

        public void DisplayBackupJobs()
        {
            List<BackupJob> jobs = backupManager.GetBackupJobs();
            if (jobs == null || jobs.Count == 0)
            {
                Console.WriteLine(languageProvider.GetText("ui.no.backups"));
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
