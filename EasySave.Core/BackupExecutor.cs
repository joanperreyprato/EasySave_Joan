using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

using EasyLog;

namespace EasySave.Core
{
    public class BackupExecutor
    {
        private readonly ILogger logger;
        private readonly StateManager stateManager;
        private readonly BusinessSoftwareMonitor? businessMonitor;
        // Global semaphore to ensure only one large file transfer at a time across all executors
        private static readonly SemaphoreSlim LargeFileSemaphore = new SemaphoreSlim(1, 1);
        private readonly long _largeFileThresholdBytes;
        private readonly List<string> _priorityExtensions;
        // Global tracking of pending priority files across all jobs
        private static readonly object PriorityLock = new object();
        private static int PendingPriorityFilesCount = 0;

        // largeFileThresholdKb: 0 disables the limit. Otherwise files larger than this (in KB) are considered "large".
        public BackupExecutor(ILogger logger, StateManager stateManager, BusinessSoftwareMonitor? businessMonitor = null, int largeFileThresholdKb = 0, List<string>? priorityExtensions = null)
        {
            this.logger = logger;
            this.stateManager = stateManager;
            this.businessMonitor = businessMonitor;
            _largeFileThresholdBytes = (largeFileThresholdKb <= 0) ? 0 : (largeFileThresholdKb * 1024L);
            _priorityExtensions = priorityExtensions ?? new List<string>();
        }

        public void ExecuteFullBackup(BackupJob job)
        {
            ArgumentNullException.ThrowIfNull(job);

            string source = job.GetSourcePath();
            string target = job.GetTargetPath();
            JobState state = job.GetState();

            // Check if business software is running
            if (businessMonitor != null && businessMonitor.IsBusinessSoftwareRunning())
            {
                state.Status = StateStatus.Error;
                state.Timestamp = DateTime.UtcNow;
                stateManager.UpdateJobState(job.GetName(), state);
                stateManager.SaveState();

                // Log the error
                try
                {
                    var runningProcesses = businessMonitor.GetRunningMonitoredProcesses();
                    string processNames = string.Join(", ", runningProcesses);
                    Console.WriteLine($"Backup blocked: Business software running ({processNames})");
                }
                catch { }

                return;
            }

            List<FileInfo> files = GetEligibleFiles(source, target, BackupType.Full);

            // Separate files into priority and non-priority
            var (priorityFiles, nonPriorityFiles) = SeparateFilesByPriority(files);

            state.TotalFiles = files.Count;
            state.TotalSize = CalculateTotalSize(files);
            state.RemainingFiles = state.TotalFiles;
            state.RemainingSize = state.TotalSize;
            state.Status = StateStatus.Active;
            state.Timestamp = DateTime.UtcNow;
            stateManager.UpdateJobState(job.GetName(), state);
            stateManager.SaveState();

            long totalTransferMs = 0;

            // Register priority files count
            if (priorityFiles.Count > 0)
            {
                lock (PriorityLock)
                {
                    PendingPriorityFilesCount += priorityFiles.Count;
                }
            }

            // Process priority files first
            foreach (var fi in priorityFiles)
            {
                totalTransferMs += ProcessSingleFile(job, fi, source, target, state);

                // Decrement priority counter after processing
                lock (PriorityLock)
                {
                    PendingPriorityFilesCount--;
                }
            }

            // Process non-priority files only if no priority files are pending globally
            foreach (var fi in nonPriorityFiles)
            {
                // Wait if there are pending priority files in any job
                while (true)
                {
                    lock (PriorityLock)
                    {
                        if (PendingPriorityFilesCount <= 0) break;
                    }
                    System.Threading.Thread.Sleep(100); // Wait a bit before checking again
                }

                totalTransferMs += ProcessSingleFile(job, fi, source, target, state);
            }

            state.Status = StateStatus.Completed;
            state.Progress = 100.0;
            state.Timestamp = DateTime.UtcNow;
            stateManager.UpdateJobState(job.GetName(), state);
            stateManager.SaveState();

            LogEntry log = new LogEntry(job.GetName(), source, target, state.TotalSize, totalTransferMs, LogEntry.EntryType.Summary);
            try { logger?.LogAction(log); } catch { }
        }

        public void ExecuteDifferentialBackup(BackupJob job)
        {
            ArgumentNullException.ThrowIfNull(job);

            string source = job.GetSourcePath();
            string target = job.GetTargetPath();
            JobState state = job.GetState();

            // Check if business software is running
            if (businessMonitor != null && businessMonitor.IsBusinessSoftwareRunning())
            {
                state.Status = StateStatus.Error;
                state.Timestamp = DateTime.UtcNow;
                stateManager.UpdateJobState(job.GetName(), state);
                stateManager.SaveState();

                try
                {
                    var runningProcesses = businessMonitor.GetRunningMonitoredProcesses();
                    string processNames = string.Join(", ", runningProcesses);
                    Console.WriteLine($"Backup blocked: Business software running ({processNames})");
                }
                catch { }

                return;
            }

            List<FileInfo> files = GetEligibleFiles(source, target, BackupType.Differential);

            // Separate files into priority and non-priority
            var (priorityFiles, nonPriorityFiles) = SeparateFilesByPriority(files);

            state.TotalFiles = files.Count;
            state.TotalSize = CalculateTotalSize(files);
            state.RemainingFiles = state.TotalFiles;
            state.RemainingSize = state.TotalSize;
            state.Status = StateStatus.Active;
            state.Timestamp = DateTime.UtcNow;
            stateManager.UpdateJobState(job.GetName(), state);

            long totalTransferMs = 0;

            // Register priority files count
            if (priorityFiles.Count > 0)
            {
                lock (PriorityLock)
                {
                    PendingPriorityFilesCount += priorityFiles.Count;
                }
            }

            // Process priority files first
            foreach (var fi in priorityFiles)
            {
                totalTransferMs += ProcessSingleFile(job, fi, source, target, state);

                // Decrement priority counter after processing
                lock (PriorityLock)
                {
                    PendingPriorityFilesCount--;
                }
            }

            // Process non-priority files only if no priority files are pending globally
            foreach (var fi in nonPriorityFiles)
            {
                // Wait if there are pending priority files in any job
                while (true)
                {
                    lock (PriorityLock)
                    {
                        if (PendingPriorityFilesCount <= 0) break;
                    }
                    System.Threading.Thread.Sleep(100); // Wait a bit before checking again
                }

                totalTransferMs += ProcessSingleFile(job, fi, source, target, state);
            }

            state.Status = StateStatus.Completed;
            state.Progress = 100.0;
            state.Timestamp = DateTime.UtcNow;
            stateManager.UpdateJobState(job.GetName(), state);
            stateManager.SaveState();

            LogEntry log = new LogEntry(job.GetName(), source, target, state.TotalSize, totalTransferMs, LogEntry.EntryType.Summary);
            try { logger?.LogAction(log); } catch { }
        }

        private long CopyFile(string source, string destination)
        {
            var sw = Stopwatch.StartNew();
            bool acquired = false;
            try
            {
                try
                {
                    // Determine file size and if it's considered "large" according to threshold
                    long fsize = 0;
                    try { fsize = new FileInfo(source).Length; } catch { fsize = 0; }

                    if (_largeFileThresholdBytes > 0 && fsize > _largeFileThresholdBytes)
                    {
                        // Wait until no other large file transfer is in progress
                        LargeFileSemaphore.Wait();
                        acquired = true;
                    }
                }
                catch
                {
                    // ignore any sizing errors and proceed to copy
                }

                File.Copy(source, destination, true);
                sw.Stop();
                return sw.ElapsedMilliseconds;
            }
            catch
            {
                try { sw.Stop(); } catch { }
                // indicate error with negative transfer time
                return -1;
            }
            finally
            {
                if (acquired)
                {
                    try { LargeFileSemaphore.Release(); } catch { }
                }
            }
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

        private (List<FileInfo> priority, List<FileInfo> nonPriority) SeparateFilesByPriority(List<FileInfo> files)
        {
            var priorityFiles = new List<FileInfo>();
            var nonPriorityFiles = new List<FileInfo>();

            if (_priorityExtensions == null || _priorityExtensions.Count == 0)
            {
                // No priority extensions defined, all files are non-priority
                return (priorityFiles, files);
            }

            foreach (var file in files)
            {
                string ext = Path.GetExtension(file.FullName).ToLowerInvariant();
                if (_priorityExtensions.Contains(ext))
                {
                    priorityFiles.Add(file);
                }
                else
                {
                    nonPriorityFiles.Add(file);
                }
            }

            return (priorityFiles, nonPriorityFiles);
        }

        private long ProcessSingleFile(BackupJob job, FileInfo fi, string source, string target, JobState state)
        {
            // Check if business software started during backup - PAUSE if detected
            if (businessMonitor != null && businessMonitor.IsBusinessSoftwareRunning())
            {
                state.Status = StateStatus.Inactive;
                state.Timestamp = DateTime.UtcNow;
                stateManager.UpdateJobState(job.GetName(), state);
                stateManager.SaveState();

                try
                {
                    var runningProcesses = businessMonitor.GetRunningMonitoredProcesses();
                    string processNames = string.Join(", ", runningProcesses);
                    Console.WriteLine($"Backup paused: Business software detected ({processNames})");
                    Console.WriteLine("Waiting for business software to close...");
                }
                catch { }

                // Wait until business software is closed
                while (businessMonitor.IsBusinessSoftwareRunning())
                {
                    System.Threading.Thread.Sleep(1000); // Check every second
                }

                // Resume backup
                state.Status = StateStatus.Active;
                state.Timestamp = DateTime.UtcNow;
                stateManager.UpdateJobState(job.GetName(), state);
                stateManager.SaveState();

                try
                {
                    Console.WriteLine("Business software closed. Resuming backup...");
                }
                catch { }
            }

            string relative = Path.GetRelativePath(source, fi.FullName);
            string dest = Path.Combine(target, relative);
            CreateDirectory(dest);

            state.CurrentSourceFile = fi.FullName;
            state.CurrentTargetFile = dest;

            long elapsed = CopyFile(fi.FullName, dest);

            // Log per-file action (transfer time negative indicates error)
            try
            {
                LogEntry fileLog = new LogEntry(job.GetName(), fi.FullName, dest, fi.Length, elapsed, LogEntry.EntryType.File);
                logger?.LogAction(fileLog);
            }
            catch
            {
                // ignore logging errors
            }

            state.RemainingFiles = Math.Max(0, state.RemainingFiles - 1);
            state.RemainingSize = Math.Max(0, state.RemainingSize - fi.Length);
            state.UpdateProgress(state.RemainingFiles, state.RemainingSize);
            stateManager.UpdateJobState(job.GetName(), state);
            stateManager.SaveState();

            // Return elapsed time for accumulation (only positive values)
            return elapsed > 0 ? elapsed : 0;
        }
    }
}
