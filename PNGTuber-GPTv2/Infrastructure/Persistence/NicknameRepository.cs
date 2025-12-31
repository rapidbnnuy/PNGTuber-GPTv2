using System;
using System.Threading;
using System.Threading.Tasks;
using LiteDB;
using PNGTuber_GPTv2.Core.Interfaces;
using PNGTuber_GPTv2.Infrastructure.Persistence;

namespace PNGTuber_GPTv2.Infrastructure.Persistence
{
    public class NicknameRepository : INicknameRepository
    {
        private readonly ICacheService _cache;
        private readonly ILogger _logger;
        private readonly string _dbPath;

        public NicknameRepository(ICacheService cache, ILogger logger, string dbPath)
        {
            _cache = cache;
            _logger = logger;
            _dbPath = dbPath;
        }

        public Task<string> GetNicknameAsync(string userId, CancellationToken ct)
        {
            // 1. L1 Cache
            var cacheKey = $"nick_{userId}";
            if (_cache.Exists(cacheKey))
            {
                return Task.FromResult(_cache.Get<string>(cacheKey));
            }

            // 2. L2 Database
            string nickname = null;

            using (var mutex = new DatabaseMutex(_logger))
            {
                if (mutex.Acquire(TimeSpan.FromSeconds(2)))
                {
                    try
                    {
                        using (var db = new LiteDatabase($"Filename={_dbPath}"))
                        {
                            var col = db.GetCollection<BsonDocument>("user_nicknames");
                            var doc = col.FindOne(x => x["UserId"] == userId);

                            if (doc != null && doc.ContainsKey("Nickname"))
                            {
                                nickname = doc["Nickname"].AsString;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"[NicknameRepo] Read Failed: {ex.Message}");
                    }
                }
            }

            // Update Cache (even if null, to avoid DB spam? strict null vs "no nickname" difference matters?)
            // For now, if null, we don't cache "null" unless we define a "NoNickname" constant.
            // Let's cache the result regardless to save DB hits.
            if (nickname != null)
            {
                _cache.Set(cacheKey, nickname, TimeSpan.FromMinutes(30));
            }

            return Task.FromResult(nickname);
        }

        public Task SetNicknameAsync(string userId, string nickname, CancellationToken ct)
        {
            // 1. DB Write
            using (var mutex = new DatabaseMutex(_logger))
            {
                if (mutex.Acquire(TimeSpan.FromSeconds(2)))
                {
                    try
                    {
                        using (var db = new LiteDatabase($"Filename={_dbPath}"))
                        {
                            var col = db.GetCollection<BsonDocument>("user_nicknames");
                            
                            // Supports multi-word because it's just a string value
                            var doc = new BsonDocument
                            {
                                ["UserId"] = userId,
                                ["Nickname"] = nickname,
                                ["UpdatedAt"] = DateTime.UtcNow
                            };

                            col.Upsert(doc);
                            col.EnsureIndex("UserId");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"[NicknameRepo] Write Failed: {ex.Message}");
                    }
                }
            }

            // 2. Cache Update
            var cacheKey = $"nick_{userId}";
            _cache.Set(cacheKey, nickname, TimeSpan.FromMinutes(30));

            return Task.CompletedTask;
        }
    }
}
