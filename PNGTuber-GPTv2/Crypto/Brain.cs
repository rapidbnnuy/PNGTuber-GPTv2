using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;
using PNGTuber_GPTv2.Core.Interfaces;
using PNGTuber_GPTv2.Domain.DTOs;

namespace PNGTuber_GPTv2.Crypto
{
    public class Brain
    {
        private readonly ILogger _logger;
        private readonly ICacheService _cache;
        private readonly List<IPipelineStep> _steps;
        
        private readonly Channel<string> _processingQueue;

        public Brain(ILogger logger, ICacheService cache, IEnumerable<IPipelineStep> steps)
        {
            _logger = logger;
            _cache = cache;
            _steps = new List<IPipelineStep>(steps);
            
            _processingQueue = Channel.CreateUnbounded<string>();
        }

        public void StartProcessing(CancellationToken ct)
        {
            Task.Run(async () => await ProcessQueueAsync(ct), ct);
        }

        public void Ingest(Dictionary<string, object> args)
        {
            try
            {
                var context = new RequestContext
                {
                    RawArgs = args,
                    RequestId = Guid.NewGuid().ToString("N"),
                    CreatedAt = DateTime.UtcNow
                };


                if (args.TryGetValue("commandId", out var cmdId))
                {
                    context.EventType = "Command";
                    context.CommandId = cmdId.ToString();
                }
                else if (args.ContainsKey("message"))
                {
                    context.EventType = "Chat";
                }
                else
                {
                    context.EventType = "Unknown";
                }

                _cache.Set($"req_{context.RequestId}", context, TimeSpan.FromMinutes(10));
                _processingQueue.Writer.TryWrite(context.RequestId);
                
                _logger.Debug($"[Brain] Ingested {context.EventType} ({context.RequestId})");
            }
            catch (Exception ex)
            {
                _logger.Error($"[Brain] Ingestion Failed: {ex.Message}");
            }
        }

        private async Task ProcessQueueAsync(CancellationToken ct)
        {
            _logger.Info("[Brain] Pipeline Processor Started.");

            try
            {
                while (await _processingQueue.Reader.WaitToReadAsync(ct))
                {
                    while (_processingQueue.Reader.TryRead(out var contextId))
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
