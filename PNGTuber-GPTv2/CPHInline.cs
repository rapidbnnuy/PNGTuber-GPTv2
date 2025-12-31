using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Streamer.bot.Plugin.Interface;
using PNGTuber_GPTv2.Core.Interfaces;
using PNGTuber_GPTv2.Crypto;
using PNGTuber_GPTv2.Infrastructure.Caching;
using PNGTuber_GPTv2.Infrastructure.External;
using PNGTuber_GPTv2.Infrastructure.FileSystem;
using PNGTuber_GPTv2.Infrastructure.Logging;
using PNGTuber_GPTv2.Infrastructure.Persistence;
using PNGTuber_GPTv2.Consumers.Identity;

namespace PNGTuber_GPTv2
{
    public class CPHInline
    {
        public IInlineInvokeProxy CPH { get; set; }

        // Singleton Instances (Static to persist across Execute calls)
        private static Brain _brain;
        private static ILogger _logger;
        private static ICacheService _cache;
        private static readonly object _lock = new object();
        private static CancellationTokenSource _globalCts;

        public bool Execute()
        {
            lock (_lock)
            {
                if (_brain == null)
                {
                    Bootstrap();
                }
            }

            if (_brain == null) return false;

            var eventArgs = new Dictionary<string, object>();
            
            TryAddVar(eventArgs, "user");
            TryAddVar(eventArgs, "userName");
            TryAddVar(eventArgs, "userId"); 
            TryAddVar(eventArgs, "display_name");
            
            TryAddVar(eventArgs, "message");
            TryAddVar(eventArgs, "rawInput");
            TryAddVar(eventArgs, "command");

            try 
            {
                eventArgs["timestamp"] = DateTime.UtcNow;
            } 
            catch { }

            _brain.Ingest(eventArgs);
            return true;
        }

        private void TryAddVar(Dictionary<string, object> dict, string key)
        {
            try 
            {
                if (CPH.TryGetArg<object>(key, out var val))
                {
                    dict[key] = val;
                }
            }
            catch {}
        }

        private void Bootstrap()
        {
            try
            {
                string dbPathRaw = CPH.GetGlobalVar<string>("Database Path", true);
                string pluginDir = Bootstrapper.Initialize(dbPathRaw);
                string dbFile = Path.Combine(pluginDir, "pngtuber.db");
                
                string logLevelStr = CPH.GetGlobalVar<string>("Logging Level", true) ?? "INFO";
                if (!Enum.TryParse(logLevelStr, true, out PNGTuber_GPTv2.Domain.Enums.LogLevel level))
                    level = PNGTuber_GPTv2.Domain.Enums.LogLevel.Info;

                _logger = new FileLogger(dbPathRaw, level);
                _logger.Info("Bootstrapping PNGTuber-GPTv2 Brain...");

                var dbBoot = new DatabaseBootstrapper(pluginDir, _logger);
                dbBoot.Initialize();
                dbBoot.PruneLockFile(); 

                _cache = new MemoryCacheService(_logger);
                var pronounApi = new AlejoPronounService(_logger);
                var pronounRepo = new PronounRepository(_cache, _logger, pronounApi, dbFile);
                var nickRepo = new NicknameRepository(_cache, _logger, dbFile);

                var steps = new List<IPipelineStep>
                {
                    new IdentityStep(_cache, pronounRepo, nickRepo, _logger)
                };

                _brain = new Brain(_logger, _cache, steps);
                _globalCts = new CancellationTokenSource();
                _brain.StartProcessing(_globalCts.Token);

                _logger.Info("Brain Online. Pipeline Ready.");
            }
            catch (Exception ex)
            {
                if (_logger != null) _logger.Error($"Bootstrap Failed: {ex}");
                _brain = null;
            }
        }
        
        // Helper to stop if needed (Trigger via separate Action)
        public bool Shutdown()
        {
             lock (_lock)
             {
                 if (_globalCts != null)
                 {
                     _globalCts.Cancel();
                     _brain = null;
                     _logger?.Info("Brain Shutdown.");
                 }
             }
             return true;
        }
    }
}
