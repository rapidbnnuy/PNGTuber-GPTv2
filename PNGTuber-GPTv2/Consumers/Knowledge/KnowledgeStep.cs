using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PNGTuber_GPTv2.Core.Interfaces;
using PNGTuber_GPTv2.Domain.DTOs;

namespace PNGTuber_GPTv2.Consumers.Knowledge
{
    public class KnowledgeStep : IPipelineStep
    {
        private readonly ICacheService _cache;
        private readonly IKnowledgeRepository _knowledge;
        private readonly ILogger _logger;

        // Simple cache for keys to avoid DB scan on every message?
        // For now, fetch all. L2 cache handles speed.
        
        public KnowledgeStep(ICacheService cache, IKnowledgeRepository knowledge, ILogger logger)
        {
            _cache = cache;
            _knowledge = knowledge;
            _logger = logger;
        }

        public async Task ExecuteAsync(string contextId, CancellationToken ct)
        {
            var key = $"req_{contextId}";
            var context = _cache.Get<RequestContext>(key);

            if (context == null || string.IsNullOrWhiteSpace(context.CleanedMessage)) return;

            try
            {
                // 1. Fetch all known facts (Small DB assumption)
                var allFacts = await _knowledge.GetAllFactsAsync(ct);
                
                // 2. Scan message for keys
                var msg = context.CleanedMessage.ToLowerInvariant();
                var hits = new List<string>();

                foreach (var fact in allFacts)
                {
                    // "rust" in "I love Rust" -> Match.
                    // "c#" in "Coding C#" -> Match. 
                    // Boundary check? "bee" in "been" -> False match?
                    // Simple contains for now.
                    if (msg.Contains(fact.Key))
                    {
                        hits.Add(fact.Content); // "Rust is a systems language..."
                    }
                }

                if (hits.Any())
                {
                   context.Facts.AddRange(hits);
                   _logger.Debug($"[KnowledgeStep] Injected {hits.Count} facts.");
                   _cache.Set(key, context, TimeSpan.FromMinutes(10));
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"[KnowledgeStep] Failed: {ex.Message}");
            }
        }
    }
}
