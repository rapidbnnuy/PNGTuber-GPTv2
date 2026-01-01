using System;
using System.Threading;
using System.Security.AccessControl;
using System.Security.Principal;
using PNGTuber_GPTv2.Core.Interfaces;

namespace PNGTuber_GPTv2.Infrastructure.Persistence
{
    public class DatabaseMutex : IDisposable
    {
        private Mutex _mutex;
        private bool _hasHandle = false;
        private readonly ILogger _logger;
        private const string MutexName = "Global\\PNGTuber-GPTv2-DB-Lock";

        public DatabaseMutex(ILogger logger)
        {
            _logger = logger;
        }

        public bool Acquire(TimeSpan timeout)
        {
            try
            {
                InitializeMutex();
                _hasHandle = _mutex.WaitOne(timeout);
                LogAcquisitionResult();
                return _hasHandle;
            }
            catch (AbandonedMutexException)
            {
                HandleAbandonedMutex();
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"DatabaseMutex: Failed to acquire: {ex}");
                return false;
            }
        }

        private void InitializeMutex()
        {
            var name = Environment.GetEnvironmentVariable("PNGTUBER_TEST_MUTEX") ?? MutexName;
            
            bool createdNew;
            _mutex = new Mutex(false, name, out createdNew);
        }

        private void LogAcquisitionResult()
        {
             if (_hasHandle) _logger.Debug("Database Mutex acquired.");
             else _logger.Warn("Failed to acquire Database Mutex (Timeout).");
        }

        private void HandleAbandonedMutex()
        {
            _hasHandle = true;
            _logger.Warn("Database Mutex was abandoned by previous owner. Lock acquired, but verify data integrity.");
        }

        public void Release()
        {
            if (_hasHandle && _mutex != null)
            {
                _mutex.ReleaseMutex();
                _hasHandle = false;
                _logger.Debug("Database Mutex released.");
            }
        }

        public void Dispose()
        {
            Release();
            _mutex?.Dispose();
        }
    }
}
