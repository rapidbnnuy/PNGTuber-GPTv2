using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LiteDB;
using PNGTuber_GPTv2.Core.Interfaces;
using PNGTuber_GPTv2.Domain.Entities;

namespace PNGTuber_GPTv2.Infrastructure.Persistence
{
    public class KnowledgeRepository : IKnowledgeRepository
    {
        private readonly ICacheService _cache;
        private readonly ILogger _logger;
        private readonly string _dbPath;
        private const string COLLECTION_NAME = "knowledge_base";

        public KnowledgeRepository(ICacheService cache, ILogger logger, string dbPath)
        {
            _cache = cache;
            _logger = logger;
            _dbPath = dbPath;
        }

        public Task AddFactAsync(string key, string content, string userId, CancellationToken ct)
        {
            return Task.Run(() =>
            {
                using (var mutex = new DatabaseMutex(_logger))
                {
                    try { UpsertFact(key, content, userId); }
                    catch (Exception ex) { _logger.Error($"[KnowledgeRepo] Add Failed: {ex.Message}"); }
                }
            }, ct);
        }

        private void UpsertFact(string key, string content, string userId)
        {
            using (var db = new LiteDatabase($"Filename={_dbPath}"))
            {
                var col = db.GetCollection<KnowledgeEntry>(COLLECTION_NAME);
                var cleanKey = key.Trim().ToLowerInvariant();
                var existing = col.FindOne(x => x.Key == cleanKey);

                if (existing != null)
                {
                    existing.Content = content;
                    existing.CreatedBy = userId;
                    existing.CreatedAt = DateTime.UtcNow;
                    col.Update(existing);
                }
                else
                {
                    col.Insert(new KnowledgeEntry { Key = cleanKey, Content = content, CreatedBy = userId });
                    col.EnsureIndex(x => x.Key);
                }

                _cache.Remove("knowledge_all");
                _cache.Set($"fact_{cleanKey}", content, TimeSpan.FromDays(7));
            }
        }

        public Task RemoveFactAsync(string key, CancellationToken ct)
        {
            return Task.Run(() =>
            {
                using (var mutex = new DatabaseMutex(_logger))
                {
                    try
                    {
                        using (var db = new LiteDatabase($"Filename={_dbPath}"))
                        {
                            var col = db.GetCollection<KnowledgeEntry>(COLLECTION_NAME);
                            col.DeleteMany(x => x.Key == key.Trim().ToLowerInvariant());
                            
                            _cache.Remove("knowledge_all");
                            _cache.Remove($"fact_{key.Trim().ToLowerInvariant()}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"[KnowledgeRepo] Remove Failed: {ex.Message}");
                    }
                }
            }, ct);
        }

        public Task<List<KnowledgeEntry>> SearchFactsAsync(string query, CancellationToken ct)
        {
            return Task.Run(() =>
            {
                using (var mutex = new DatabaseMutex(_logger))
                {
                    try
                    {
                        using (var db = new LiteDatabase($"Filename={_dbPath}"))
                        {
                            var col = db.GetCollection<KnowledgeEntry>(COLLECTION_NAME);
                            var cleanQuery = query.Trim().ToLowerInvariant();
                            
                            return col.Find(x => x.Key == cleanQuery).ToList();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"[KnowledgeRepo] Search Failed: {ex.Message}");
                        return new List<KnowledgeEntry>();
                    }
                }
            }, ct);
        }

        public Task<List<KnowledgeEntry>> GetAllFactsAsync(CancellationToken ct)
        {
            return Task.Run(() =>
            {
                if (_cache.Exists("knowledge_all")) return _cache.Get<List<KnowledgeEntry>>("knowledge_all");

                using (var mutex = new DatabaseMutex(_logger))
                {
                    try
                    {
                        using (var db = new LiteDatabase($"Filename={_dbPath}"))
                        {
                            var col = db.GetCollection<KnowledgeEntry>(COLLECTION_NAME);
                            var all = col.FindAll().ToList();
                            _cache.Set("knowledge_all", all, TimeSpan.FromMinutes(60));
                            return all;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"[KnowledgeRepo] GetAll Failed: {ex.Message}");
                        return new List<KnowledgeEntry>();
                    }
                }
            }, ct);
        }
    }
}
