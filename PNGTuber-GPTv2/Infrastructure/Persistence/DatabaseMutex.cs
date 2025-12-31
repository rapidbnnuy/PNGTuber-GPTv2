using System;
using System.Threading;
using PNGTuber_GPTv2.Core.Interfaces;

namespace PNGTuber_GPTv2.Infrastructure.Persistence
{
    public class DatabaseMutex : IDisposable
    {
        private readonly Mutex _mutex;
        private bool _hasHandle = false;
        private readonly ILogger _logger;

        // Unique name for the application/plugin
        private const string MutexName = "Global\\PNGTuber-GPTv2-DB-Lock";

        public DatabaseMutex(ILogger logger)
        {
            _logger = logger;
            // Create or open the named mutex.
            // InitiallyOwned: false
            _mutex = new Mutex(false, MutexName);
        }

        public bool Acquire(TimeSpan timeout)
        {
            try
            {
                _logger.Debug($"Acquiring Database Mutex ({MutexName})...");
                _hasHandle = _mutex.WaitOne(timeout);
                
                if (_hasHandle)
                    _logger.Debug("Database Mutex acquired.");
                else
                    _logger.Warn("Failed to acquire Database Mutex (Timeout).");

                return _hasHandle;
            }
            catch (AbandonedMutexException)
            {
                // If a previous process crashed without releasing, we get this.
                // We now own the mutex, but state might be inconsistent.
                _hasHandle = true;
                _logger.Warn("Database Mutex was abandoned by previous owner. Lock acquired, but verify data integrity.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error acquiring Database Mutex: {ex.Message}");
                return false;
            }
        }

        public void Release()
        {
            if (_hasHandle)
            {
                _mutex.ReleaseMutex();
                _hasHandle = false;
                _logger.Debug("Database Mutex released.");
            }
        }

        public void Dispose()
        {
            Release();
            _mutex.Dispose();
        }
    }
}
