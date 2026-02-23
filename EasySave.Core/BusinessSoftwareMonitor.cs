using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace EasySave.Core
{
    /// <summary>
    /// Monitors if business software processes are running
    /// </summary>
    public class BusinessSoftwareMonitor
    {
        private readonly List<string> monitoredProcesses;

        public BusinessSoftwareMonitor()
        {
            monitoredProcesses = new List<string>();
        }

        public BusinessSoftwareMonitor(IEnumerable<string> processNames)
        {
            monitoredProcesses = new List<string>(processNames ?? Enumerable.Empty<string>());
        }

        /// <summary>
        /// Add a process name to monitor (without .exe extension)
        /// </summary>
        public void AddProcess(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName)) return;

            // Remove .exe if present
            string cleanName = processName.Replace(".exe", "", StringComparison.OrdinalIgnoreCase).Trim();
            
            if (!monitoredProcesses.Contains(cleanName, StringComparer.OrdinalIgnoreCase))
            {
                monitoredProcesses.Add(cleanName);
            }
        }

        /// <summary>
        /// Remove a process name from monitoring
        /// </summary>
        public void RemoveProcess(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName)) return;

            string cleanName = processName.Replace(".exe", "", StringComparison.OrdinalIgnoreCase).Trim();
            monitoredProcesses.RemoveAll(p => string.Equals(p, cleanName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Clear all monitored processes
        /// </summary>
        public void ClearProcesses()
        {
            monitoredProcesses.Clear();
        }

        /// <summary>
        /// Get the list of monitored process names
        /// </summary>
        public IReadOnlyList<string> GetMonitoredProcesses()
        {
            return monitoredProcesses.AsReadOnly();
        }

        /// <summary>
        /// Check if any monitored business software is currently running
        /// </summary>
        /// <returns>True if any monitored process is running, false otherwise</returns>
        public bool IsBusinessSoftwareRunning()
        {
            if (monitoredProcesses.Count == 0)
                return false;

            try
            {
                Process[] runningProcesses = Process.GetProcesses();
                
                foreach (string monitoredProcess in monitoredProcesses)
                {
                    if (runningProcesses.Any(p => 
                        string.Equals(p.ProcessName, monitoredProcess, StringComparison.OrdinalIgnoreCase)))
                    {
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                // If we can't check processes, assume it's safe to proceed
                return false;
            }
        }

        /// <summary>
        /// Get the list of monitored processes that are currently running
        /// </summary>
        public List<string> GetRunningMonitoredProcesses()
        {
            List<string> running = new List<string>();
            
            if (monitoredProcesses.Count == 0)
                return running;

            try
            {
                Process[] runningProcesses = Process.GetProcesses();
                
                foreach (string monitoredProcess in monitoredProcesses)
                {
                    if (runningProcesses.Any(p => 
                        string.Equals(p.ProcessName, monitoredProcess, StringComparison.OrdinalIgnoreCase)))
                    {
                        running.Add(monitoredProcess);
                    }
                }
            }
            catch
            {
                // Ignore errors
            }

            return running;
        }
    }
}
