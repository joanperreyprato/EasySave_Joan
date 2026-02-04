using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using EasyLog;

namespace EasySave_v1._0
{
    public class BackupExecutor
    {
        private readonly ILogger logger;
        private readonly StateManager stateManager;

        public BackupExecutor(ILogger logger, StateManager stateManager)
        {
            this.logger = logger;
            this.stateManager = stateManager;
        }

        public void ExecuteFullBackup(BackupJob job)
        {
            ArgumentNullException.ThrowIfNull(job);

            string source = job.GetSourcePath();
            string target = job.GetTargetPath();
            JobState state = job.GetState();

            List<FileInfo> files = GetEligibleFiles(source, target, BackupType.Full);
            state.TotalFiles = files.Count;
            state.TotalSize = CalculateTotalSize(files);
            state.RemainingFiles = state.TotalFiles;
            state.RemainingSize = state.TotalSize;
            state.Status = StateStatus.Active;
            state.Timestamp = DateTime.UtcNow;
            stateManager.UpdateJobState(job.GetName(), state);
            stateManager.SaveState();

            long totalTransferMs = 0;

            foreach (var fi in files)
            {
                string relative = Path.GetRelativePath(source, fi.FullName);
                string dest = Path.Combine(target, relative);
                CreateDirectory(dest);

                state.CurrentSourceFile = fi.FullName;
                state.CurrentTargetFile = dest;

                long elapsed = CopyFile(fi.FullName, dest);
                totalTransferMs += elapsed;

                state.RemainingFiles = Math.Max(0, state.RemainingFiles - 1);
                state.RemainingSize = Math.Max(0, state.RemainingSize - fi.Length);
                state.UpdateProgress(state.RemainingFiles, state.RemainingSize);
                stateManager.UpdateJobState(job.GetName(), state);
                stateManager.SaveState();
            }

            state.Status = StateStatus.Completed;
            state.Progress = 100.0;
            state.Timestamp = DateTime.UtcNow;
            stateManager.UpdateJobState(job.GetName(), state);
            stateManager.SaveState();

            LogEntry log = new LogEntry(job.GetName(), source, target, state.TotalSize, totalTransferMs);
            try { logger?.LogAction(log); } catch { }
        }

        public void ExecuteDifferentialBackup(BackupJob job)
        {
            ArgumentNullException.ThrowIfNull(job);

            string source = job.GetSourcePath();
            string target = job.GetTargetPath();
            JobState state = job.GetState();

            List<FileInfo> files = GetEligibleFiles(source, target, BackupType.Differential);
            state.TotalFiles = files.Count;
            state.TotalSize = CalculateTotalSize(files);
            state.RemainingFiles = state.TotalFiles;
            state.RemainingSize = state.TotalSize;
            state.Status = StateStatus.Active;
            state.Timestamp = DateTime.UtcNow;
            stateManager.UpdateJobState(job.GetName(), state);

            long totalTransferMs = 0;

            foreach (var fi in files)
            {
                string relative = Path.GetRelativePath(source, fi.FullName);
                string dest = Path.Combine(target, relative);
                CreateDirectory(dest);

                state.CurrentSourceFile = fi.FullName;
                state.CurrentTargetFile = dest;

                long elapsed = CopyFile(fi.FullName, dest);
                totalTransferMs += elapsed;

                state.RemainingFiles = Math.Max(0, state.RemainingFiles - 1);
                state.RemainingSize = Math.Max(0, state.RemainingSize - fi.Length);
                state.UpdateProgress(state.RemainingFiles, state.RemainingSize);
                stateManager.UpdateJobState(job.GetName(), state);
            }

            state.Status = StateStatus.Completed;
            state.Progress = 100.0;
            state.Timestamp = DateTime.UtcNow;
            stateManager.UpdateJobState(job.GetName(), state);
            stateManager.SaveState();

            LogEntry log = new LogEntry(job.GetName(), source, target, state.TotalSize, totalTransferMs);
            try { logger?.LogAction(log); } catch { }
        }

        private static long CopyFile(string source, string destination)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                File.Copy(source, destination, true);
            }
            catch
            {
                // ignore individual copy errors
            }
            sw.Stop();
            return sw.ElapsedMilliseconds;
        }

        private static void CreateDirectory(string path)
        {
            try
            {
                string? dir = Path.GetDirectoryName(path);
                if (string.IsNullOrWhiteSpace(dir)) return;
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            }
            catch
            {
                // ignore
            }
        }

        private static List<FileInfo> GetEligibleFiles(string sourcePath, string targetPath, BackupType type)
        {
            List<FileInfo> list = new();
            if (!Directory.Exists(sourcePath)) return list;

            string[] sourceFiles = Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories);
            foreach (var s in sourceFiles)
            {
                FileInfo fi = new FileInfo(s);
                if (type == BackupType.Full)
                {
                    list.Add(fi);
                    continue;
                }

                // Differential: include when file does not exist in target or is newer
                string relative = Path.GetRelativePath(sourcePath, s);
                string targetFile = Path.Combine(targetPath, relative);
                if (!File.Exists(targetFile))
                {
                    list.Add(fi);
                    continue;
                }

                try
                {
                    FileInfo tfi = new FileInfo(targetFile);
                    if (fi.LastWriteTimeUtc > tfi.LastWriteTimeUtc || fi.Length != tfi.Length)
                    {
                        list.Add(fi);
                    }
                }
                catch
                {
                    list.Add(fi);
                }
            }

            return list;
        }

        private static long CalculateTotalSize(List<FileInfo> files)
        {
            if (files == null || files.Count == 0) return 0;
            long total = 0;
            foreach (var f in files)
            {
                try { total += f.Length; } catch { }
            }
            return total;
        }
    }
}
