using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PNGTuber_GPTv2.Core.Interfaces;
using PNGTuber_GPTv2.Domain.DTOs;

namespace PNGTuber_GPTv2.Consumers.Knowledge
{
    public class UserKnowledgeService : IConsumer
    {
        private readonly ICacheService _cache;
        private readonly IKnowledgeRepository _knowledge;
        private readonly ILogger _logger;
        private readonly IProcessingChannel<string> _inChannel;
        private readonly IProcessingChannel<string> _outChannel;

        public UserKnowledgeService(
            ICacheService cache, 
            IKnowledgeRepository knowledge, 
            ILogger logger,
            IProcessingChannel<string> inChannel,
            IProcessingChannel<string> outChannel)
        {
            _cache = cache;
            _knowledge = knowledge;
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
            _logger.Info("[KnowledgeService] Started.");
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
            catch (Exception ex) { _logger.Error($"[KnowledgeService] Crash: {ex.Message}"); }
        }

        private async Task ProcessAsync(string contextId, CancellationToken ct)
        {
            var key = $"req_{contextId}";
            var context = _cache.Get<RequestContext>(key);
            if (context == null || string.IsNullOrWhiteSpace(context.CleanedMessage)) 
            {
                if (context != null) _outChannel.TryWrite(contextId);
                return;
            }

            try
            {
                var hits = await ScanKnowledgeBase(context.CleanedMessage, ct);
                if (hits.Any())
                {
                   context.Facts.AddRange(hits);
                   _logger.Debug($"[KnowledgeService] Injected {hits.Count} facts.");
                   _cache.Set(key, context, TimeSpan.FromMinutes(10));
                }
                
                _outChannel.TryWrite(contextId);
            }
            catch (Exception ex)
            {
                _logger.Error($"[KnowledgeService] Failed: {ex.Message}");
            }
        }

        private async Task<List<string>> ScanKnowledgeBase(string message, CancellationToken ct)
        {
            var allFacts = await _knowledge.GetAllFactsAsync(ct);
            var msg = message.ToLowerInvariant();
            var hits = new List<string>();

            foreach (var fact in allFacts)
            {
                if (msg.Contains(fact.Key)) hits.Add(fact.Content);
            }
            return hits;
        }
    }
}
