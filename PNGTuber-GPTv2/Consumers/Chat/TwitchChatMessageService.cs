using System;
using System.Threading;
using System.Threading.Tasks;
using PNGTuber_GPTv2.Core.Interfaces;
using PNGTuber_GPTv2.Domain.DTOs;
using PNGTuber_GPTv2.Domain.Entities;

namespace PNGTuber_GPTv2.Consumers.Chat
{
    public class TwitchChatMessageService : IConsumer
    {
        private readonly ICacheService _cache;
        private readonly IChatMessageRepository _repo;
        private readonly ILogger _logger;
        private readonly IProcessingChannel<string> _inChannel;

        public TwitchChatMessageService(
            ICacheService cache, 
            IChatMessageRepository repo, 
            ILogger logger,
            IProcessingChannel<string> inChannel)
        {
            _cache = cache;
            _repo = repo;
            _logger = logger;
            _inChannel = inChannel;
        }

        public Task StartAsync(CancellationToken ct)
        {
            return Task.Run(async () => await RunLoopAsync(ct), ct);
        }

        private async Task RunLoopAsync(CancellationToken ct)
        {
            _logger.Info("[ChatPersistence] Started.");
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
            catch (Exception ex) { _logger.Error($"[ChatPersistence] Crash: {ex.Message}"); }
        }

        private async Task ProcessAsync(string contextId, CancellationToken ct)
        {
            var key = $"req_{contextId}";
            var context = _cache.Get<RequestContext>(key);

            if (context == null || context.User == null) return;
            if (context.EventType != "Chat" || string.IsNullOrWhiteSpace(context.CleanedMessage)) return;

            ExtractUserInfo(context, out var name, out var userId);
            
            var msg = new ChatMessage
            {
                UserId = userId,
                DisplayName = name,
                Message = context.CleanedMessage,
                Timestamp = context.CreatedAt,
                FullText = $"{name} said {context.CleanedMessage}"
            };

            await PersistMessageAsync(msg);
        }

        private void ExtractUserInfo(RequestContext context, out string name, out string userId)
        {
            name = "Unknown";
            userId = "Unknown";
            if (context.RawArgs != null && 
                context.RawArgs.TryGetValue("user", out var u) && 
                context.RawArgs.TryGetValue("userId", out var uid))
            {
                name = u.ToString();
                userId = uid.ToString();
            }
        }

        private async Task PersistMessageAsync(ChatMessage msg)
        {
            try 
            {
                await _repo.AddAsync(msg);
                _logger.Info($"[ChatPersistence] Saved message from {msg.DisplayName}");
            }
            catch (Exception ex)
            {
                _logger.Error($"[ChatPersistence] DB Write Failed: {ex.Message}");
            }
        }
    }
}
