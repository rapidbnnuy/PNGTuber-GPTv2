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
        private readonly INicknameRepository _nicknames;
        private readonly ILogger _logger;

        public IdentityStep(ICacheService cache, IPronounRepository pronouns, INicknameRepository nicknames, ILogger logger)
        {
            _cache = cache;
            _pronouns = pronouns;
            _nicknames = nicknames;
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

                    // 2. Resolve Pronouns (Cache -> DB -> API)
                    var pronouns = await _pronouns.GetPronounsAsync(userId, displayName, ct);

                    // 3. Resolve Nickname (Cache -> DB)
                    string nickname = await _nicknames.GetNicknameAsync(userId, ct);

                    // 4. Update Context
                    context.User = new User 
                    { 
                        Id = userId,
                        DisplayName = displayName,
                        Nickname = nickname, // Can be null
                        FirstSeen = DateTime.UtcNow 
                    };
                    context.Pronouns = pronouns;

                    _logger.Debug($"[IdentityStep] {displayName} ({nickname ?? "No Nick"}) - {pronouns.Display}");
                }
                else
                {
                    _logger.Debug("[IdentityStep] No user info in args. Skipping identity.");
                }

                // 5. Save Enriched Context
                _cache.Set(key, context, TimeSpan.FromMinutes(10));
            }
            catch (Exception ex)
            {
                _logger.Error($"[IdentityStep] Failed: {ex.Message}");
            }
        }
    }
}
