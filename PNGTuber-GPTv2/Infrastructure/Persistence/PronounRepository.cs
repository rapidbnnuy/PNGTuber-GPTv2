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
            // 1. L1 Cache (Fastest)
            var cacheKey = $"pronouns_{userId}";
            if (_cache.Exists(cacheKey))
            {
                return _cache.Get<Pronouns>(cacheKey);
            }

            // 2. L2 Database (Exclusive Lock)
            Pronouns? dbPronouns = null;
            bool needsUpdate = false;
            
            // We read DB state first. Note: We use Mutex even for reads if we want strict consistency,
            // but for performance, shared read is often okay if supported.
            // However, our Mutex wrapper is global. To be safe, we acquire it.
            using (var mutex = new DatabaseMutex(_logger))
            {
                if (mutex.Acquire(TimeSpan.FromSeconds(2)))
                {
                    try
                    {
                        using (var db = new LiteDatabase($"Filename={_dbPath}"))
                        {
                            var col = db.GetCollection<BsonDocument>("user_pronouns");
                            var doc = col.FindOne(x => x["UserId"] == userId);

                            if (doc != null)
                            {
                                // Parse from DB
                                dbPronouns = new Pronouns(
                                    doc["Display"].AsString,
                                    doc["Subject"].AsString,
                                    doc["Object"].AsString,
                                    doc["Possessive"].AsString,
                                    doc["PossessivePronoun"].AsString,
                                    doc["Reflexive"].AsString,
                                    doc["PastTense"].AsString,
                                    doc["CurrentTense"].AsString,
                                    doc["Plural"].AsBoolean
                                );
                                
                                // Check Freshness (7 days)
                                var lastUpdated = doc["LastUpdated"].AsDateTime;
                                if ((DateTime.UtcNow - lastUpdated).TotalDays > 7)
                                {
                                    needsUpdate = true;
                                }
                            }
                            else
                            {
                                needsUpdate = true;
                            }
                        }
                    }
                    catch (Exception ex) 
                    {
                        _logger.Error($"DB Read Failed: {ex.Message}");
                    }
                }
            }

            if (dbPronouns.HasValue && !needsUpdate)
            {
                // Found and fresh -> Update L1 -> Return
                _cache.Set(cacheKey, dbPronouns.Value, TimeSpan.FromMinutes(30));
                return dbPronouns.Value;
            }

            // 3. L3 API (Slow Path)
            // If we are here, it's missing or stale. We fetch from Alejo.
            _logger.Info($"Fetching fresh pronouns for {displayName} ({userId})...");
            var freshPronouns = await _api.FetchPronounsAsync(userId, ct);

            // If API failed, fallback to DB value if we had one, or Default.
            var finalPronouns = freshPronouns ?? dbPronouns ?? Pronouns.TheyThem;

            // 4. Write Back (L2 + L1)
            using (var mutex = new DatabaseMutex(_logger))
            {
                if (mutex.Acquire(TimeSpan.FromSeconds(2)))
                {
                    try
                    {
                        using (var db = new LiteDatabase($"Filename={_dbPath}"))
                        {
                            var col = db.GetCollection<BsonDocument>("user_pronouns");
                            
                            var doc = new BsonDocument
                            {
                                ["UserId"] = userId,
                                ["Display"] = finalPronouns.Display,
                                ["Subject"] = finalPronouns.Subject,
                                ["Object"] = finalPronouns.Object,
                                ["Possessive"] = finalPronouns.Possessive,
                                ["PossessivePronoun"] = finalPronouns.PossessivePronoun,
                                ["Reflexive"] = finalPronouns.Reflexive,
                                ["PastTense"] = finalPronouns.PastTense,
                                ["CurrentTense"] = finalPronouns.CurrentTense,
                                ["Plural"] = finalPronouns.Plural,
                                ["LastUpdated"] = DateTime.UtcNow
                            };

                            col.Upsert(doc);
                            col.EnsureIndex("UserId");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"DB Write Failed: {ex.Message}");
                    }
                }
            }

            _cache.Set(cacheKey, finalPronouns, TimeSpan.FromMinutes(30));
            return finalPronouns;
        }
    }
}
