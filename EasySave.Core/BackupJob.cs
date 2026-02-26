using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EasyLog;

namespace EasySave.Core
{
    public class BackupJob
    {
        private readonly string name;
        private readonly string sourcePath;
        private readonly string targetPath;
        private readonly BackupType type;
        private readonly EasyLog.LogFormat logFormat;

        private JobState state;

        public BackupJob(string name, string source, string target, BackupType type, EasyLog.LogFormat logFormat = EasyLog.LogFormat.Json)
        {
            this.name = name ?? string.Empty;
            this.sourcePath = source ?? string.Empty;
            this.targetPath = target ?? string.Empty;
            this.type = type;
            this.logFormat = logFormat;

            this.state = new JobState(this.name)
            {
                CurrentSourceFile = string.Empty,
                CurrentTargetFile = string.Empty,
                Status = StateStatus.Inactive
            };
        }

        // Execute the backup: perform a simple file copy from source to target and update state
        public void Execute(EasyLog.ILogger logger, StateManager stateManager)
        {
            if (stateManager == null) throw new ArgumentNullException(nameof(stateManager));

            // Validate source
            if (!Directory.Exists(sourcePath))
            {
                state.Status = StateStatus.Error;
                state.Timestamp = DateTime.UtcNow;
                stateManager.UpdateJobState(name, state);
                stateManager.SaveState();
                return;
            }

            string[] files = Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories);
            state.TotalFiles = files.Length;
            state.TotalSize = files.Select(f => new FileInfo(f).Length).Sum();
            state.RemainingFiles = state.TotalFiles;
            state.RemainingSize = state.TotalSize;
            state.Status = StateStatus.Active;
            state.Timestamp = DateTime.UtcNow;
            stateManager.UpdateJobState(name, state);
            stateManager.SaveState();

            Stopwatch sw = Stopwatch.StartNew();

            try
            {
                foreach (var file in files)
                {
                    string relative = Path.GetRelativePath(sourcePath, file);
                    string destPath = Path.Combine(targetPath, relative);
                    string destDir = Path.GetDirectoryName(destPath) ?? string.Empty;
                    if (!Directory.Exists(destDir))
                        Directory.CreateDirectory(destDir);

                    state.CurrentSourceFile = file;
                    state.CurrentTargetFile = destPath;

                    long transferMs = 0;
                    Stopwatch fileSw = Stopwatch.StartNew();
                    try
                    {
                        File.Copy(file, destPath, true);
                        fileSw.Stop();
                        transferMs = fileSw.ElapsedMilliseconds;
                    }
                    catch
                    {
                        // mark as error for this file by using negative transfer time
                        fileSw.Stop();
                        transferMs = -1;
                    }

                    // Update remaining counts/sizes (keep existing behaviour)
                    FileInfo fi = new FileInfo(file);
                    state.RemainingFiles = Math.Max(0, state.RemainingFiles - 1);
                    state.RemainingSize = Math.Max(0, state.RemainingSize - fi.Length);
                    state.Timestamp = DateTime.UtcNow;
                    state.UpdateProgress(state.RemainingFiles, state.RemainingSize);
                    stateManager.UpdateJobState(name, state);
                    // Persist state in real-time
                    try { stateManager.SaveState(); } catch { }

                    // Log this file action (size and transfer time; negative if error)
                    try
                    {
                        LogEntry fileLog = new LogEntry(name, file, destPath, fi.Length, transferMs, LogEntry.EntryType.File);
                        logger.LogAction(fileLog);
                    }
                    catch
                    {
                        // ignore logging errors and continue
                    }
                }

                sw.Stop();
                state.Status = StateStatus.Completed;
                state.Timestamp = DateTime.UtcNow;
                state.Progress = 100.0;
                stateManager.UpdateJobState(name, state);
                stateManager.SaveState();

                // Log overall completion
                LogEntry log = new LogEntry(name, sourcePath, targetPath, state.TotalSize, sw.ElapsedMilliseconds, LogEntry.EntryType.Summary);
                try
                {
                    logger?.LogAction(log);
                }
                catch
                {
                    // ignore logging errors
                }
            }
            catch
            {
                sw.Stop();
                state.Status = StateStatus.Error;
                state.Timestamp = DateTime.UtcNow;
                stateManager.UpdateJobState(name, state);
                stateManager.SaveState();
            }
        }

        public string GetName() => name;
        public string GetSourcePath() => sourcePath;
        public string GetTargetPath() => targetPath;
        public BackupType GetBackupType() => type;
        public JobState GetState() => state;
        public EasyLog.LogFormat GetLogFormat() => logFormat;
    }
}
