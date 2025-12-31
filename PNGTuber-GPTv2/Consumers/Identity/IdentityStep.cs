using System;
using System.Threading;
using System.Threading.Tasks;
using PNGTuber_GPTv2.Core.Interfaces;
using PNGTuber_GPTv2.Domain.DTOs;
using PNGTuber_GPTv2.Domain.Entities;

namespace PNGTuber_GPTv2.Consumers.Identity
{
    public class IdentityStep : IPipelineStep
    {
        private readonly ICacheService _cache;
        private readonly IPronounRepository _pronouns;
        private readonly ILogger _logger;

        public IdentityStep(ICacheService cache, IPronounRepository pronouns, ILogger logger)
        {
            _cache = cache;
            _pronouns = pronouns;
            _logger = logger;
        }

        public async Task ExecuteAsync(string contextId, CancellationToken ct)
        {
            var key = $"req_{contextId}";
            var context = _cache.Get<RequestContext>(key);

            if (context == null) 
            {
                _logger.Warn($"[IdentityStep] Context {contextId} not found in cache.");
                return;
            }

            try
            {
                // 1. Resolve User ID & Name from Args
                if (context.RawArgs.TryGetValue("userId", out var uidObj) && 
                    context.RawArgs.TryGetValue("user", out var nameObj))
                {
                    var userId = uidObj.ToString();
                    var displayName = nameObj.ToString();

                    // 2. Resolve Pronouns via Repository (Cache -> DB -> API)
                    var pronouns = await _pronouns.GetPronounsAsync(userId, displayName, ct);

                    // 3. Update Context
                    context.User = new User 
                    { 
                        Id = userId,
                        DisplayName = displayName,
                        FirstSeen = DateTime.UtcNow 
                    };
                    context.Pronouns = pronouns;

                    _logger.Debug($"[IdentityStep] ID: {displayName}, Pronouns: {pronouns.Display}");
                }
                else
                {
                    _logger.Debug("[IdentityStep] No user info in args. Skipping identity.");
                }

                // 4. Save Enriched Context
                _cache.Set(key, context, TimeSpan.FromMinutes(10));
            }
            catch (Exception ex)
            {
                _logger.Error($"[IdentityStep] Failed: {ex.Message}");
            }
        }
    }
}
