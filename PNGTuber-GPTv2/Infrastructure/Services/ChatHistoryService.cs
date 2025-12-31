using System.Collections.Generic;
using System.Linq;
using PNGTuber_GPTv2.Core.Interfaces;

namespace PNGTuber_GPTv2.Infrastructure.Services
{
    public class ChatHistoryService : IChatHistoryService
    {
        private readonly ICacheService _cache;
        private readonly List<string> _buffer;
        private readonly object _lock = new object();
        private const int MAX_HISTORY = 20;
        private const string CACHE_KEY = "chat_history";

        public ChatHistoryService(ICacheService cache)
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

                // Update KV Store for external consumers (e.g. LLM Prompt builder)
                // We clone the list to avoid concurrency issues during serialization/storage?
                // Depending on Cache implementation. MemoryCache stores reference? 
                // If MemoryCache stores reference, modification here modifies cache.
                // But safer to assume we should just set it properly.
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
