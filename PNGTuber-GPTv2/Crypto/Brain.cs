using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PNGTuber_GPTv2.Core.Interfaces;
using PNGTuber_GPTv2.Domain.DTOs;

namespace PNGTuber_GPTv2.Crypto
{
    public class Brain
    {
        private readonly ILogger _logger;
        private readonly ICacheService _cache;
        private readonly List<IPipelineStep> _steps;
        
        private readonly IProcessingQueue _queue;

        public Brain(ILogger logger, ICacheService cache, IEnumerable<IPipelineStep> steps, IProcessingQueue queue)
        {
            _logger = logger;
            _cache = cache;
            _steps = new List<IPipelineStep>(steps);
            _queue = queue;
        }

        public void StartProcessing(CancellationToken ct)
        {
            Task.Run(async () => await ProcessQueueAsync(ct), ct);
        }

        public void Ingest(Dictionary<string, object> args)
        {
            if (args == null) return;
            try
            {
                var context = CreateContext(args);
                _cache.Set($"req_{context.RequestId}", context, TimeSpan.FromMinutes(10));
                
                if (_queue.TryEnqueue(context.RequestId))
                {
                    if (context.EventType != "Unknown")
                        _logger.Info($"[Brain] Ingested {context.EventType}: {context.CleanedMessage}");
                }
                else
                {
                    _logger.Error($"[Brain] Failed to enqueue request {context.RequestId}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"[Brain] Ingest Failed: {ex.Message}");
            }
        }

        private RequestContext CreateContext(Dictionary<string, object> args)
        {
            var eventType = "Unknown";
            var message = "";
            var commandId = "";

            if (args.ContainsKey("commandId")) 
            {
                eventType = "Command";
                commandId = args["commandId"]?.ToString();
                message = args.ContainsKey("rawInput") ? args["rawInput"]?.ToString() : "";
            }
            else if (args.ContainsKey("message"))
            {
                eventType = "Chat";
                message = args["message"]?.ToString();
            }

            return new RequestContext 
            { 
                RequestId = Guid.NewGuid().ToString(),
                RawArgs = args,
                EventType = eventType,
                CommandId = commandId,
                CleanedMessage = message,
                CreatedAt = DateTime.UtcNow
            };
        }

        private async Task ProcessQueueAsync(CancellationToken ct)
        {
            _logger.Info("[Brain] Pipeline Processor Started.");

            try
            {
                while (await _queue.WaitToReadAsync(ct))
                {
                    while (_queue.TryDequeue(out var contextId))
                    {
                        await RunPipelineAsync(contextId, ct);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.Info("[Brain] Processing Stopped (Cancelled).");
            }
            catch (Exception ex)
            {
                _logger.Error($"[Brain] Processor Crash: {ex.Message}");
            }
        }

        private async Task RunPipelineAsync(string contextId, CancellationToken ct)
        {
            using (var stepScope = new CancellationTokenSource(TimeSpan.FromSeconds(60))) 
            {
                var combinedCt = CancellationTokenSource.CreateLinkedTokenSource(ct, stepScope.Token).Token;

                try
                {
                    foreach (var step in _steps)
                    {
                        if (combinedCt.IsCancellationRequested) break;

                        await step.ExecuteAsync(contextId, combinedCt);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"[Brain] Pipeline Error for {contextId}: {ex.Message}");
                }
            }
        }
    }
}
