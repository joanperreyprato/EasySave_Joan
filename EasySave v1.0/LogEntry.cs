using EasyLog;

namespace EasySave_v1._0
{
    // Thin wrapper to keep compatibility with original project namespace
    public class LogEntry : EasyLog.LogEntry
    {
        public LogEntry(string backupName, string source, string target, long size, long transferTime)
            : base(backupName, source, target, size, transferTime)
        {
        }
    }
}
