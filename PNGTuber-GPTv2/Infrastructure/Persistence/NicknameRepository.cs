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
            var cacheKey = $"nick_{userId}";
            if (_cache.Exists(cacheKey)) return Task.FromResult(_cache.Get<string>(cacheKey));

            string nickname = FetchNickFromDb(userId);

            if (nickname != null)
                _cache.Set(cacheKey, nickname, TimeSpan.FromMinutes(30));

            return Task.FromResult(nickname);
        }

        private string FetchNickFromDb(string userId)
        {
            using (var mutex = new DatabaseMutex(_logger))
            {
                if (!mutex.Acquire(TimeSpan.FromSeconds(5))) return null;
                try
                {
                    using (var db = new LiteDatabase($"Filename={_dbPath}"))
                    {
                        var col = db.GetCollection<BsonDocument>("user_nicknames");
                        var doc = col.FindOne(x => x["UserId"] == userId);
                        return (doc != null && doc.ContainsKey("Nickname")) ? doc["Nickname"].AsString : null;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"[NicknameRepo] Read Failed: {ex.Message}");
                    return null;
                }
            }
        }

        public Task SetNicknameAsync(string userId, string nickname, CancellationToken ct)
        {
            UpsertNick(userId, nickname);
            var cacheKey = $"nick_{userId}";
            _cache.Set(cacheKey, nickname, TimeSpan.FromMinutes(30));
            return Task.CompletedTask;
        }

        private void UpsertNick(string userId, string nickname)
        {
             using (var mutex = new DatabaseMutex(_logger))
            {
                if (!mutex.Acquire(TimeSpan.FromSeconds(5))) return;
                try
                {
                    using (var db = new LiteDatabase($"Filename={_dbPath}"))
                    {
                         var col = db.GetCollection<BsonDocument>("user_nicknames");
                         var existing = col.FindOne(x => x["UserId"] == userId);
                         if (existing != null)
                         {
                             existing["Nickname"] = nickname;
                             existing["UpdatedAt"] = DateTime.UtcNow;
                             col.Update(existing);
                         }
                         else
                         {
                             col.Insert(new BsonDocument { ["UserId"] = userId, ["Nickname"] = nickname, ["UpdatedAt"] = DateTime.UtcNow });
                             col.EnsureIndex("UserId");
                         }
                    }
                }
                catch (Exception ex) { _logger.Error($"[NicknameRepo] Write Failed: {ex.Message}"); }
            }
        }
    }
}
