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
        
        // The Queue holds Context IDs, not full objects.
        private readonly Channel<string> _processingQueue;

        public Brain(ILogger logger, ICacheService cache, IEnumerable<IPipelineStep> steps)
        {
            _logger = logger;
            _cache = cache;
            _steps = new List<IPipelineStep>(steps);
            
            // Unbounded queue for high throughput
            _processingQueue = Channel.CreateUnbounded<string>();
        }

        public void StartProcessing(CancellationToken ct)
        {
            Task.Run(async () => await ProcessQueueAsync(ct), ct);
        }

        // 1. Ingestion Point (Main Thread)
        public void Ingest(Dictionary<string, object> args)
        {
            try
            {
                // Create Context
                var context = new RequestContext
                {
                    RawArgs = args
                };

                // Save to Cache
                _cache.Set($"req_{context.RequestId}", context, TimeSpan.FromMinutes(10));

                // Push ID to Queue
                _processingQueue.Writer.TryWrite(context.RequestId);
                
                _logger.Debug($"[Brain] Ingested Request {context.RequestId}");
            }
            catch (Exception ex)
            {
                _logger.Error($"[Brain] Ingestion Failed: {ex.Message}");
            }
        }

        // 2. Processing Loop (Background Thread)
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

        // 3. Sequential Execution (The Pipeline)
        private async Task RunPipelineAsync(string contextId, CancellationToken ct)
        {
            using (var stepScope = new CancellationTokenSource(TimeSpan.FromSeconds(60))) // Hard timeout per request
            {
                var combinedCt = CancellationTokenSource.CreateLinkedTokenSource(ct, stepScope.Token).Token;

                try
                {
                    foreach (var step in _steps)
                    {
                        if (combinedCt.IsCancellationRequested) break;

                        // Execute Step (Step is responsible for Fetch/Mutate/Save)
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
