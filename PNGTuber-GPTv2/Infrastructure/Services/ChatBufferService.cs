using System.Collections.Generic;
using System.Linq;
using PNGTuber_GPTv2.Core.Interfaces;

namespace PNGTuber_GPTv2.Infrastructure.Services
{
    public class ChatBufferService : IChatBufferService
    {
        private readonly ICacheService _cache;
        private readonly List<string> _buffer;
        private readonly object _lock = new object();
        private const int MAX_HISTORY = 20;
        private const string CACHE_KEY = "chat_history";

        public ChatBufferService(ICacheService cache)
        {
            _cache = cache;
            _buffer = new List<string>();
        }

        public void AddMessage(string formattedMessage)
        {
            lock (_lock)
            {
                _buffer.Add(formattedMessage);
                
                while (_buffer.Count > MAX_HISTORY)
                {
                    _buffer.RemoveAt(0);
                }

                _cache.Set(CACHE_KEY, new List<string>(_buffer), System.TimeSpan.FromDays(1));
            }
        }

        public List<string> GetRecentMessages()
        {
            lock (_lock)
            {
                return new List<string>(_buffer);
            }
        }
    }
}
