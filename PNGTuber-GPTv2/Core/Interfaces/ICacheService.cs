using System;

namespace PNGTuber_GPTv2.Core.Interfaces
{
    public interface ICacheService
    {
        T Get<T>(string key);
        void Set<T>(string key, T value, TimeSpan? duration = null);
        void Remove(string key);
        bool Exists(string key);
    }
}
