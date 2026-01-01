using System;
using System.Threading;
using System.Threading.Tasks;
using LiteDB;
using PNGTuber_GPTv2.Core.Interfaces;
using PNGTuber_GPTv2.Domain.Entities;
using PNGTuber_GPTv2.Domain.Structs;

namespace PNGTuber_GPTv2.Infrastructure.Persistence
{
    public class PronounRepository : IPronounRepository
    {
        private readonly ICacheService _cache;
        private readonly ILogger _logger;
        private readonly IPronounService _api;
        private readonly string _dbPath;

        public PronounRepository(ICacheService cache, ILogger logger, IPronounService api, string dbPath)
        {
            _cache = cache;
            _logger = logger;
            _api = api;
            _dbPath = dbPath;
        }

        public async Task<Pronouns> GetPronounsAsync(string userId, string displayName, CancellationToken ct)
        {
            var cacheKey = $"pronouns_{userId}";
            if (_cache.Exists(cacheKey)) return _cache.Get<Pronouns>(cacheKey);

            var (dbPronouns, isFresh) = TryGetFromDatabase(userId);
            if (isFresh && dbPronouns.HasValue)
            {
                _cache.Set(cacheKey, dbPronouns.Value, TimeSpan.FromMinutes(30));
                return dbPronouns.Value;
            }

            var apiPronouns = await FetchFromApi(userId, displayName, ct);
            
            var finalPronouns = apiPronouns ?? dbPronouns ?? Pronouns.Default;

            if (apiPronouns.HasValue) UpdateDatabase(userId, finalPronouns);

            _cache.Set(cacheKey, finalPronouns, TimeSpan.FromMinutes(30));
            return finalPronouns;
        }

        private (Pronouns? result, bool isFresh) TryGetFromDatabase(string userId)
        {
            using (var mutex = new DatabaseMutex(_logger))
            {
                if (!mutex.Acquire(TimeSpan.FromSeconds(2))) return (null, false);
                try
                {
                    return ReadPronounsFromDb(userId);
                }
                catch (Exception ex)
                {
                    _logger.Error($"DB Read Failed: {ex.Message}");
                    return (null, false);
                }
            }
        }

        private (Pronouns? result, bool isFresh) ReadPronounsFromDb(string userId)
        {
            using (var db = new LiteDatabase($"Filename={_dbPath}"))
            {
                var col = db.GetCollection<BsonDocument>("user_pronouns");
                var doc = col.FindOne(x => x["UserId"] == userId);
                if (doc == null) return (null, false);

                var p = new Pronouns(
                    doc["Display"].AsString, doc["Subject"].AsString, doc["Object"].AsString,
                    doc["Possessive"].AsString, doc["PossessivePronoun"].AsString, doc["Reflexive"].AsString,
                    doc["PastTense"].AsString, doc["CurrentTense"].AsString, doc["Plural"].AsBoolean
                );
                var isFresh = (DateTime.UtcNow - doc["LastUpdated"].AsDateTime).TotalDays < 7;
                return (p, isFresh);
            }
        }

        private async Task<Pronouns?> FetchFromApi(string userId, string displayName, CancellationToken ct)
        {
            _logger.Info($"Fetching fresh pronouns for {displayName} ({userId})...");
            return await _api.FetchPronounsAsync(displayName.ToLower(), ct) ?? await _api.FetchPronounsAsync(userId, ct);
        }

        private void UpdateDatabase(string userId, Pronouns p)
        {
            using (var mutex = new DatabaseMutex(_logger))
            {
                if (!mutex.Acquire(TimeSpan.FromSeconds(2))) return;
                try { WritePronounsToDb(userId, p); }
                catch (Exception ex) { _logger.Error($"DB Write Failed: {ex.Message}"); }
            }
        }
        
        private void WritePronounsToDb(string userId, Pronouns p)
        {
            using (var db = new LiteDatabase($"Filename={_dbPath}"))
            {
                var col = db.GetCollection<BsonDocument>("user_pronouns");
                var doc = new BsonDocument
                {
                    ["UserId"] = userId, ["Display"] = p.Display, ["Subject"] = p.Subject,
                    ["Object"] = p.Object, ["Possessive"] = p.Possessive, ["PossessivePronoun"] = p.PossessivePronoun,
                    ["Reflexive"] = p.Reflexive, ["PastTense"] = p.PastTense, ["CurrentTense"] = p.CurrentTense,
                    ["Plural"] = p.Plural, ["LastUpdated"] = DateTime.UtcNow
                };
                col.Upsert(doc);
                col.EnsureIndex("UserId");
            }
        }
    }
}
