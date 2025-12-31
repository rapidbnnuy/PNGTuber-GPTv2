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
using PNGTuber_GPTv2.Consumers.Command;
using PNGTuber_GPTv2.Domain.Enums;

namespace PNGTuber_GPTv2.Core
{
    public class BotEngine
    {
        private readonly IStreamerBotProxy _cph;
        private ILogger _logger;
        private Brain _brain;
        private ICacheService _cache;
        private CancellationTokenSource _globalCts;

        public BotEngine(IStreamerBotProxy cph)
        {
            _cph = cph;
        }

        public bool Start()
        {
            try
            {
                var (pluginDir, dbFile) = InitializePaths();
                _logger.Info("Bootstrapping PNGTuber-GPTv2 Engine...");

                InitializeDatabase(pluginDir);
                InitializeServices(dbFile);
                StartBrain();

                _logger.Info("Engine Online.");
                return true;
            }
            catch (Exception ex)
            {
                if (_logger != null) _logger.Error($"Bootstrap Failed: {ex}");
                else _cph.LogInfo($"Bootstrap Critical Fail: {ex}"); // Fallback
                return false;
            }
        }

        public void Ingest(Dictionary<string, object> args)
        {
            if (_brain == null) return;
            
            // Normalize Args
            try { args["timestamp"] = DateTime.UtcNow; } catch { }
            
            _brain.Ingest(args);
        }

        public void Shutdown()
        {
            if (_globalCts != null)
            {
                _globalCts.Cancel();
                _brain = null;
                _logger?.Info("Engine Shutdown.");
            }
        }

        // --- Internals ---

        private (string dir, string list) InitializePaths()
        {
            string dbPathRaw = _cph.GetGlobalVar<string>("Database Path", true);
            string pluginDir = Bootstrapper.Initialize(dbPathRaw);
            
            string logLevelStr = _cph.GetGlobalVar<string>("Logging Level", true) ?? "INFO";
            if (!Enum.TryParse(logLevelStr, true, out LogLevel level))
                level = LogLevel.Info;

            _logger = new FileLogger(dbPathRaw, level);
            return (pluginDir, Path.Combine(pluginDir, "pngtuber.db"));
        }

        private void InitializeDatabase(string pluginDir)
        {
            var dbBoot = new DatabaseBootstrapper(pluginDir, _logger);
            dbBoot.Initialize();
            dbBoot.PruneLockFile(); 
        }

        private void InitializeServices(string dbFile)
        {
            _cache = new MemoryCacheService(_logger);
            var pronounApi = new AlejoPronounService(_logger);
            var pronounRepo = new PronounRepository(_cache, _logger, pronounApi, dbFile);
            var nickRepo = new NicknameRepository(_cache, _logger, dbFile);

            var steps = new List<IPipelineStep>
            {
                new IdentityStep(_cache, pronounRepo, nickRepo, _logger),
                new CommandStep(_cache, nickRepo, _logger)
            };
            
            _brain = new Brain(_logger, _cache, steps);
        }

        private void StartBrain()
        {
            _globalCts = new CancellationTokenSource();
            _brain.StartProcessing(_globalCts.Token);
        }
    }
}
