using System;
using System.Threading;
using System.Threading.Tasks;
using DataAccessLibrary; // Replace with your actual data access library namespace

namespace LoggingLibrary
{
    // Remove the LogLevel enum if it's no longer needed

    public class LogEntry
    {
        public long LogID { get; set; } // Assuming this is generated by the database
        public string UserHash { get; set; }
        public string ActionType { get; set; }
        public DateTime LogTime { get; set; }
        public string LogStatus { get; set; }
        public string LogDetail { get; set; }

        public LogEntry(string userHash, string actionType, string logStatus, string logDetail)
        {
            LogTime = DateTime.UtcNow;
            UserHash = userHash;
            ActionType = actionType;
            LogStatus = logStatus;
            LogDetail = logDetail;
        }
    }

    public interface ILogger
    {
        Task LogAsync(LogEntry entry);
        Task ArchiveLogsAsync(); // Implement as needed
    }

    public class Logger : ILogger
    {
        private readonly IDataAccess _dataAccess;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public Logger(IDataAccess dataAccess)
        {
            _dataAccess = dataAccess;
        }

        public async Task LogAsync(LogEntry entry)
        {
            await _semaphore.WaitAsync();
            try
            {
                // Check for null userHash and log an error message if needed
                if (string.IsNullOrWhiteSpace(entry.UserHash))
                {
                    var errorEntry = new LogEntry("Unknown", "Error", "Failure", 
                        "Null user hash detected for operation");
                    await _dataAccess.SaveLogAsync(errorEntry);
                }
                else
                {
                    // Save the log entry to the database
                    await _dataAccess.SaveLogAsync(entry);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task ArchiveLogsAsync()
        {
            // Archive older log entries
            await _semaphore.WaitAsync();
            try
            {
                await _dataAccess.ArchiveLogsAsync();
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}