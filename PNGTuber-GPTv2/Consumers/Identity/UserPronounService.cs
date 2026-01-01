using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PNGTuber_GPTv2.Core.Interfaces;
using PNGTuber_GPTv2.Domain.DTOs;
using PNGTuber_GPTv2.Domain.Entities;

namespace PNGTuber_GPTv2.Consumers.Identity
{
    public class UserPronounService : IConsumer
    {
        private readonly ICacheService _cache;
        private readonly IPronounRepository _pronouns;
        private readonly INicknameRepository _nicks;
        private readonly ILogger _logger;
        private readonly IProcessingChannel<string> _inChannel;
        private readonly IProcessingChannel<string> _outChannel;

        public UserPronounService(
            ICacheService cache, 
            IPronounRepository pronouns, 
            INicknameRepository nicks, 
            ILogger logger,
            IProcessingChannel<string> inChannel,
            IProcessingChannel<string> outChannel)
        {
            _cache = cache;
            _pronouns = pronouns;
            _nicks = nicks;
            _logger = logger;
            _inChannel = inChannel;
            _outChannel = outChannel;
        }

        public Task StartAsync(CancellationToken ct)
        {
            return Task.Run(async () => await RunLoopAsync(ct), ct);
        }

        private async Task RunLoopAsync(CancellationToken ct)
        {
            _logger.Info("[IdentityService] Started.");
            try 
            {
                while (await _inChannel.WaitToReadAsync(ct))
                {
                    while (_inChannel.TryRead(out var contextId))
                    {
                        await ProcessAsync(contextId, ct);
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { _logger.Error($"[IdentityService] Crash: {ex.Message}"); }
        }

        private async Task ProcessAsync(string contextId, CancellationToken ct)
        {
            var key = $"req_{contextId}";
            var context = _cache.Get<RequestContext>(key);
            if (context == null) return;

            try
            {
                await EnrichIdentityAsync(context, ct);
                
                _cache.Set(key, context, TimeSpan.FromMinutes(10));
                _outChannel.TryWrite(contextId);
            }
            catch (Exception ex)
            {
                _logger.Error($"[IdentityService] Failed for {contextId}: {ex.Message}");
            }
        }

        private async Task EnrichIdentityAsync(RequestContext context, CancellationToken ct)
        {
            if (TryExtractUserInfo(context.RawArgs, out var userId, out var displayName))
            {
                var pronouns = await _pronouns.GetPronounsAsync(userId, displayName, ct);
                var nickname = await _nicks.GetNicknameAsync(userId, ct);

                context.User = new User 
                { 
                    Id = userId,
                    DisplayName = displayName,
                    Nickname = nickname, 
                    FirstSeen = DateTime.UtcNow 
                };
                context.Pronouns = pronouns;
                _logger.Debug($"[IdentityService] Resolved: {displayName} ({pronouns.Display})");
            }
        }

        private bool TryExtractUserInfo(Dictionary<string, object> args, out string userId, out string displayName)
        {
            userId = null;
            displayName = null;
            if (args != null && args.TryGetValue("userId", out var uidObj) && args.TryGetValue("user", out var nameObj))
            {
                userId = uidObj?.ToString();
                displayName = nameObj?.ToString();
                return !string.IsNullOrEmpty(userId);
            }
            return false;
        }
    }
}
