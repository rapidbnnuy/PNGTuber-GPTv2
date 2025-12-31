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
        private readonly ILogger _logger;

        public IdentityStep(ICacheService cache, ILogger logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public Task ExecuteAsync(string contextId, CancellationToken ct)
        {
            var key = $"req_{contextId}";
            var context = _cache.Get<RequestContext>(key);

            if (context == null) 
            {
                _logger.Warn($"[IdentityStep] Context {contextId} not found in cache.");
                return Task.CompletedTask;
            }

            try
            {
                // Simulate Identity Resolution (In real app, query Repo/DB)
                if (context.RawArgs.ContainsKey("userId"))
                {
                   // Stub logic
                   context.User = new User 
                   { 
                       Id = context.RawArgs["userId"].ToString(),
                       DisplayName = "TestUser",
                       FirstSeen = DateTime.UtcNow
                   };
                   _logger.Debug($"[IdentityStep] Resolved User: {context.User.Id}");
                }
                else
                {
                    _logger.Debug("[IdentityStep] No userId in args.");
                }

                // Save Enriched Context back to Cache
                _cache.Set(key, context, TimeSpan.FromMinutes(10));
            }
            catch (Exception ex)
            {
                _logger.Error($"[IdentityStep] Failed: {ex.Message}");
            }

            return Task.CompletedTask;
        }
    }
}
