using System;
using System.Threading;
using System.Threading.Tasks;
using PNGTuber_GPTv2.Core.Interfaces;
using PNGTuber_GPTv2.Domain.DTOs;

namespace PNGTuber_GPTv2.Infrastructure.Services
{
    public class RouterService : IConsumer
    {
        private readonly ICacheService _cache;
        private readonly ILogger _logger;
        private readonly IProcessingChannel<string> _inChannel;
        private readonly IProcessingChannel<string> _knowledgeOut;
        private readonly IProcessingChannel<string> _commandOut;

        public RouterService(
            ICacheService cache,
            ILogger logger,
            IProcessingChannel<string> inChannel,
            IProcessingChannel<string> knowledgeOut,
            IProcessingChannel<string> commandOut)
        {
            _cache = cache;
            _logger = logger;
            _inChannel = inChannel;
            _knowledgeOut = knowledgeOut;
            _commandOut = commandOut;
        }

        public Task StartAsync(CancellationToken ct)
        {
            return Task.Run(async () => await RunLoopAsync(ct), ct);
        }

        private async Task RunLoopAsync(CancellationToken ct)
        {
            _logger.Info("[RouterService] Started.");
            try
            {
                while (await _inChannel.WaitToReadAsync(ct))
                {
                    while (_inChannel.TryRead(out var contextId))
                    {
                        RouteContext(contextId);
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { _logger.Error($"[RouterService] Crash: {ex.Message}"); }
        }

        private void RouteContext(string contextId)
        {
            var key = $"req_{contextId}";
            var context = _cache.Get<RequestContext>(key);
            if (context == null) return;

            if (context.EventType == "Command" || 
               (!string.IsNullOrEmpty(context.CleanedMessage) && context.CleanedMessage.StartsWith("!")))
            {
                _commandOut.TryWrite(contextId);
            }
            else
            {
                _knowledgeOut.TryWrite(contextId);
            }
        }
    }
}
