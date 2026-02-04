namespace EasyLog
{
    public interface ILogger
    {
        void LogAction(LogEntry message);
        void CreateDailyLogFile();
    }
}
