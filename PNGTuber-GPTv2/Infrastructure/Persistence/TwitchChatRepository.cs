using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiteDB;
using PNGTuber_GPTv2.Core.Interfaces;
using PNGTuber_GPTv2.Domain.Entities;

namespace PNGTuber_GPTv2.Infrastructure.Persistence
{
    public class TwitchChatRepository : IChatMessageRepository
    {
        private readonly ILogger _logger;
        private readonly string _dbPath;

        public TwitchChatRepository(ILogger logger, string dbPath)
        {
            _logger = logger;
            _dbPath = dbPath;
        }

        public Task AddAsync(ChatMessage message)
        {
            using (var mutex = new DatabaseMutex(_logger))
            {
                if (!mutex.Acquire(TimeSpan.FromSeconds(2))) return Task.CompletedTask;
                try
                {
                    using (var db = new LiteDatabase($"Filename={_dbPath}"))
                    {
                        var col = db.GetCollection<ChatMessage>("chat_logs");
                        col.Insert(message);
                        col.EnsureIndex(x => x.Timestamp);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"[ChatRepo] Write Failed: {ex.Message}");
                }
                return Task.CompletedTask;
            }
        }

        public Task<List<ChatMessage>> GetRecentAsync(int count)
        {
            using (var mutex = new DatabaseMutex(_logger))
            {
                if (!mutex.Acquire(TimeSpan.FromSeconds(2))) return Task.FromResult(new List<ChatMessage>());
                try
                {
                    using (var db = new LiteDatabase($"Filename={_dbPath}"))
                    {
                        var col = db.GetCollection<ChatMessage>("chat_logs");
                        return Task.FromResult(col.Query()
                            .OrderByDescending(x => x.Timestamp)
                            .Limit(count)
                            .ToList());
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"[ChatRepo] Read Failed: {ex.Message}");
                    return Task.FromResult(new List<ChatMessage>());
                }
            }
        }
    }
}
