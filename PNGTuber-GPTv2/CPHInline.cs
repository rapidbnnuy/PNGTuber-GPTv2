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

            // Extract Context from Streamer.bot
            // We manually map known variables since we can't get the raw 'args' dict easily in DLL
            var eventArgs = new Dictionary<string, object>();
            
            // Common User Vars
            TryAddVar(eventArgs, "user");
            TryAddVar(eventArgs, "userName");
            TryAddVar(eventArgs, "userId"); // twitchId
            TryAddVar(eventArgs, "display_name");
            
            // Chat Vars
            TryAddVar(eventArgs, "message");
            TryAddVar(eventArgs, "rawInput");
            TryAddVar(eventArgs, "command");

            // Event Metadata
            try 
            {
                // Retrieve Event Source if possible, otherwise default
                eventArgs["timestamp"] = DateTime.UtcNow;
            } 
            catch { }

            _brain.Ingest(eventArgs);
            return true;
        }

        private void TryAddVar(Dictionary<string, object> dict, string key)
        {
            // CPH.GetGlobalVar is for Globals. For Check vars (local args), we use GetVar usually?
            // In DLL Interface, 'GetVar' isn't always exposed same as CPH script.
            // Using TryGetArg which is typical for Actions.
            
            // Note: The interface provided might differ slightly based on version.
            // Assuming we can't easily get local args without TryGetArg.
            // If the binding misses, we wrap safely.
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
                // 1. Config & Paths
                string dbPathRaw = CPH.GetGlobalVar<string>("Database Path", true);
                string pluginDir = Bootstrapper.Initialize(dbPathRaw);
                string dbFile = Path.Combine(pluginDir, "pngtuber.db");
                
                string logLevelStr = CPH.GetGlobalVar<string>("Logging Level", true) ?? "INFO";
                if (!Enum.TryParse(logLevelStr, true, out PNGTuber_GPTv2.Domain.Enums.LogLevel level))
                    level = PNGTuber_GPTv2.Domain.Enums.LogLevel.Info;

                // 2. Logger
                _logger = new FileLogger(dbPathRaw, level);
                _logger.Info("Bootstrapping PNGTuber-GPTv2 Brain...");

                // 3. Database Init
                var dbBoot = new DatabaseBootstrapper(pluginDir, _logger);
                dbBoot.Initialize();
                dbBoot.PruneLockFile(); // Recovery on startup

                // 4. Services
                _cache = new MemoryCacheService(_logger);
                var pronounApi = new AlejoPronounService(_logger);
                var pronounRepo = new PronounRepository(_cache, _logger, pronounApi, dbFile);
                var nickRepo = new NicknameRepository(_cache, _logger, dbFile);

                // 5. Build Pipeline Steps
                var steps = new List<IPipelineStep>
                {
                    new IdentityStep(_cache, pronounRepo, nickRepo, _logger)
                    // Future: ModerationStep, LogicStep, etc.
                };

                // 6. Start Brain
                _brain = new Brain(_logger, _cache, steps);
                _globalCts = new CancellationTokenSource();
                _brain.StartProcessing(_globalCts.Token);

                _logger.Info("Brain Online. Pipeline Ready.");
            }
            catch (Exception ex)
            {
                // If logger exists, log it. Else... we are blind.
                if (_logger != null) _logger.Error($"Bootstrap Failed: {ex}");
                // Reset so we can try again next time
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
