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
            _logDirectory = Path.Combine(basePath, "PNGTuber-GPT", "logs");
            _configuredLevel = configuredLevel;

            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }

        public void Log(string message, LogLevel level)
        {
            if ((int)level > (int)_configuredLevel)
            {
                return;
            }

            try
            {
                string logFileName = DateTime.Now.ToString("PNGTuber-GPT_yyyyMMdd") + ".log";
                string logFilePath = Path.Combine(_logDirectory, logFileName);
                
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {level.ToString().ToUpper()}] {message}{Environment.NewLine}";

                lock (_lock)
                {
                    File.AppendAllText(logFilePath, logEntry);
                }
            }
            catch
            {
            }
        }

        public void Error(string message) => Log(message, LogLevel.Error);
        public void Warn(string message) => Log(message, LogLevel.Warn);
        public void Info(string message) => Log(message, LogLevel.Info);
        public void Debug(string message) => Log(message, LogLevel.Debug);
    }
}
