using System;
using System.Runtime.Caching;
using PNGTuber_GPTv2.Core.Interfaces;

namespace PNGTuber_GPTv2.Infrastructure.Caching
{
    public class MemoryCacheService : ICacheService
    {
        private readonly MemoryCache _cache;
        private readonly ILogger _logger;
        // Default TTL if not specified
        private readonly TimeSpan _defaultDuration = TimeSpan.FromMinutes(30);

        public MemoryCacheService(ILogger logger)
        {
            _logger = logger;
            _cache = MemoryCache.Default;
        }

        public T Get<T>(string key)
        {
            try
            {
                if (_cache.Contains(key))
                {
                    var item = _cache.Get(key);
                    if (item is T typedItem)
                        return typedItem;
                }
            }
            catch (Exception ex)
            {
                _logger.Warn($"Cache Read ErrorForKey '{key}': {ex.Message}");
            }
            return default;
        }

        public void Set<T>(string key, T value, TimeSpan? duration = null)
        {
            try
            {
                if (value == null) return;

                var expiry = DateTimeOffset.Now.Add(duration ?? _defaultDuration);
                _cache.Set(key, value, expiry);
            }
            catch (Exception ex)
            {
                _logger.Warn($"Cache Write Error For Key '{key}': {ex.Message}");
            }
        }

        public void Remove(string key)
        {
            try
            {
                _cache.Remove(key);
            }
            catch (Exception ex)
            {
                _logger.Warn($"Cache Remove Error For Key '{key}': {ex.Message}");
            }
        }

        public bool Exists(string key)
        {
            return _cache.Contains(key);
        }
    }
}
