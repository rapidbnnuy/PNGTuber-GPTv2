using System.Threading;
using System.Threading.Tasks;
using PNGTuber_GPTv2.Core.Interfaces;
using PNGTuber_GPTv2.Domain.DTOs;

namespace PNGTuber_GPTv2.Consumers.History
{
    public class ChatHistoryService : IPipelineStep
    {
        private readonly ICacheService _cache;
        private readonly IChatBufferService _buffer;
        private readonly ILogger _logger;

        public ChatHistoryService(ICacheService cache, IChatBufferService buffer, ILogger logger)
        {
            _cache = cache;
            _buffer = buffer;
            _logger = logger;
        }

        public Task ExecuteAsync(string contextId, CancellationToken ct)
        {
            var key = $"req_{contextId}";
            var context = _cache.Get<RequestContext>(key);

            if (context == null || context.User == null) return Task.CompletedTask;

            if (context.EventType == "Chat" && !string.IsNullOrWhiteSpace(context.CleanedMessage))
            {
                var name = context.User.Nickname ?? context.User.DisplayName;
                var pDisplay = context.Pronouns.Display ?? "They/Them";
                
                var formatted = $"{name} ({pDisplay}) said {context.CleanedMessage}";
                
                _buffer.AddMessage(formatted);
                _logger.Info($"[History] Archived: {formatted}");
            }

            return Task.CompletedTask;
        }
    }
}
