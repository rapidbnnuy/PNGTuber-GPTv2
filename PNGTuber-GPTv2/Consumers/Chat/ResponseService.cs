using System;
using System.Threading;
using System.Threading.Tasks;
using PNGTuber_GPTv2.Core.Interfaces;
using PNGTuber_GPTv2.Domain.DTOs;

namespace PNGTuber_GPTv2.Consumers.Chat
{
    public class ResponseService : IConsumer
    {
        private readonly ICacheService _cache;
        private readonly IChatBufferService _buffer;
        private readonly ILogger _logger;
        private readonly IProcessingChannel<string> _inChannel;

        public ResponseService(
            ICacheService cache, 
            IChatBufferService buffer, 
            ILogger logger,
            IProcessingChannel<string> inChannel)
        {
            _cache = cache;
            _buffer = buffer;
            _logger = logger;
            _inChannel = inChannel;
        }

        public Task StartAsync(CancellationToken ct)
        {
            return Task.Run(async () => await RunLoopAsync(ct), ct);
        }

        private async Task RunLoopAsync(CancellationToken ct)
        {
            _logger.Info("[ResponseService] Started.");
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
            catch (Exception ex) { _logger.Error($"[ResponseService] Crash: {ex.Message}"); }
        }

        private Task ProcessAsync(string contextId, CancellationToken ct)
        {
            var key = $"req_{contextId}";
            var context = _cache.Get<RequestContext>(key);
            if (context == null) return Task.CompletedTask;

            if (!string.IsNullOrEmpty(context.GeneratedResponse))
            {
                _logger.Info($"[BOT SAYS]: {context.GeneratedResponse}");
            }

            if (context.EventType == "Chat" && !string.IsNullOrEmpty(context.CleanedMessage))
            {
                 var name = context.User?.DisplayName ?? "Unknown";
                 var nick = context.User?.Nickname ?? "None";
                 var pDisplay = context.Pronouns.Display ?? "They/Them";
                 _buffer.AddMessage($"{name} / ({nick}) ({pDisplay}) said {context.CleanedMessage}.");
            }
            return Task.CompletedTask;
        }
    }
}
