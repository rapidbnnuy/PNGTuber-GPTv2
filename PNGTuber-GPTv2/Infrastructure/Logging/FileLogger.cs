using System;
using System.IO;
using PNGTuber_GPTv2.Core.Interfaces;
using PNGTuber_GPTv2.Domain.Enums;

namespace PNGTuber_GPTv2.Infrastructure.Logging
{
    public class FileLogger : ILogger
    {
        private readonly string _logDirectory;
        private readonly LogLevel _configuredLevel;
        private readonly object _lock = new object();

        public FileLogger(string basePath, LogLevel configuredLevel)
        {
            // Ensure PNGTuber-GPT/logs structure
            _logDirectory = Path.Combine(basePath, "PNGTuber-GPT", "logs");
            _configuredLevel = configuredLevel;

            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }

        public void Log(string message, LogLevel level)
        {
            // Priority Check: Lower numeric value = Higher Priority.
            // If message level (e.g. Debug=4) > Configured (Error=1), we skip it?
            // User snippet: "if (logLevelPriority[logLevel] <= logLevelPriority[globalLogLevel])"
            // Dictionary was: Error=1, Warn=2, Info=3, Debug=4.
            // If Global=INFO(3). Msg=DEBUG(4). 4 <= 3 is False. Skip.
            // If Global=INFO(3). Msg=ERROR(1). 1 <= 3 is True. Write.
            
            if ((int)level > (int)_configuredLevel)
            {
                return;
            }

            try
            {
                string logFileName = DateTime.Now.ToString("PNGTuber-GPT_yyyyMMdd") + ".log";
                string logFilePath = Path.Combine(_logDirectory, logFileName);
                
                // Format: [2025-12-31 12:00:00.000 INFO] Message
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {level.ToString().ToUpper()}] {message}{Environment.NewLine}";

                lock (_lock)
                {
                    File.AppendAllText(logFilePath, logEntry);
                }
            }
            catch
            {
                // Fail silently or fallback? 
                // We cannot log the failure of the logger easily without CPH reference.
            }
        }

        public void Error(string message) => Log(message, LogLevel.Error);
        public void Warn(string message) => Log(message, LogLevel.Warn);
        public void Info(string message) => Log(message, LogLevel.Info);
        public void Debug(string message) => Log(message, LogLevel.Debug);
    }
}
