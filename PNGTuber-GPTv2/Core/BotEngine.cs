using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Streamer.bot.Plugin.Interface;
using PNGTuber_GPTv2.Core.Interfaces;
using PNGTuber_GPTv2.Crypto.Channels;
using PNGTuber_GPTv2.Infrastructure.Caching;
using PNGTuber_GPTv2.Infrastructure.External;
using PNGTuber_GPTv2.Infrastructure.FileSystem;
using PNGTuber_GPTv2.Infrastructure.Logging;
using PNGTuber_GPTv2.Infrastructure.Persistence;
using PNGTuber_GPTv2.Infrastructure.Services;
using PNGTuber_GPTv2.Consumers.Identity;
using PNGTuber_GPTv2.Consumers.Command;
using PNGTuber_GPTv2.Consumers.Knowledge;
using PNGTuber_GPTv2.Consumers.Chat;
using PNGTuber_GPTv2.Domain.Entities;
using PNGTuber_GPTv2.Domain.DTOs;
using PNGTuber_GPTv2.Domain.Enums;

namespace PNGTuber_GPTv2.Core
{
    public class BotEngine
    {
        private readonly IStreamerBotProxy _cph;
        private ILogger _logger;
        private ICacheService _cache;
        private CancellationTokenSource _globalCts;

        private IdentityChannel _identityChannel;
        private RouterChannel _routerChannel;
        private KnowledgeChannel _knowledgeChannel;
        private CommandChannel _commandChannel;
        private OutputChannel _outputChannel;
        private ChatPersistenceChannel _persistenceChannel;

        private List<IConsumer> _consumers;
        private List<string> _ignoreList;

        public BotEngine(IStreamerBotProxy cph)
        {
            _cph = cph;
        }

        public bool Start()
        {
            try
            {
                var (pluginDir, dbFile) = InitializePaths();
                _logger.Info("Bootstrapping PNGTuber-GPTv2 Engine (SEDA)...");
                _globalCts = new CancellationTokenSource();

                InitializeDatabase(pluginDir);
                InitializeChannels();
                InitializeServices(dbFile);
                
                CommandRegistry.RegisterAll(_cph);
                StartConsumers();

                _logger.Info("Engine Online.");
                return true;
            }
            catch (Exception ex)
            {
                if (_logger != null) _logger.Error($"Bootstrap Failed: {ex}");
                else _cph.LogInfo($"Bootstrap Critical Fail: {ex}");
                return false;
            }
        }

        public void Ingest(Dictionary<string, object> args)
        {
            if (_cache == null) return;
            try 
            {
                var context = BuildContext(args);
                if (IsIgnored(context)) return;

                var key = $"req_{context.RequestId}";
                _cache.Set(key, context, TimeSpan.FromMinutes(10));
                
                _identityChannel.TryWrite(context.RequestId);
                _persistenceChannel.TryWrite(context.RequestId);
            } 
            catch (Exception ex) 
            {
                _logger.Error($"Ingest Failed: {ex.Message}");
            }
        }

        private RequestContext BuildContext(Dictionary<string, object> args)
        {
            var ctx = new RequestContext { RawArgs = args };
            if (args.TryGetValue("eventId", out var eid)) ctx.RequestId = eid.ToString();
            if (args.TryGetValue("message", out var msg)) ctx.CleanedMessage = msg?.ToString()?.Trim();
            if (args.TryGetValue("commandId", out var cid)) ctx.CommandId = cid?.ToString();
            
            if (args.TryGetValue("triggerType", out var type)) 
            {
                var s = type.ToString();
                if (s == "TwitchChatMessage" || s == "YouTubeMessage") ctx.EventType = "Chat";
                else if (s == "Command") ctx.EventType = "Command";
                else ctx.EventType = s;
            }
            return ctx;
        }

        private bool IsIgnored(RequestContext ctx)
        {
             if (ctx.RawArgs.TryGetValue("user", out var u) && u != null)
             {
                 var name = u.ToString();
                 if (_ignoreList.Contains(name.ToLowerInvariant()))
                 {
                     _logger.Info($"Ignored event from {name}");
                     return true;
                 }
             }
             return false;
        }

        public void Shutdown()
        {
            if (_globalCts != null)
            {
                _globalCts.Cancel();
                _logger?.Info("Engine Shutdown.");
            }
        }

        private (string dir, string list) InitializePaths()
        {
            string dbPathRaw = _cph.GetGlobalVar<string>("Database Path", true);
            string pluginDir = Bootstrapper.Initialize(dbPathRaw);
            
            string logLevelStr = _cph.GetGlobalVar<string>("Logging Level", true) ?? "INFO";
            if (!Enum.TryParse(logLevelStr, true, out LogLevel level)) level = LogLevel.Info;

            string ignoreCsv = _cph.GetGlobalVar<string>("IgnoreBotNames", true) ?? "";
            _ignoreList = new List<string>();
            if (!string.IsNullOrWhiteSpace(ignoreCsv))
            {
                var names = ignoreCsv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var name in names) _ignoreList.Add(name.Trim().ToLowerInvariant());
            }

            _logger = new FileLogger(dbPathRaw, level);
            return (pluginDir, Path.Combine(pluginDir, "pngtuber.db"));
        }

        private void InitializeDatabase(string pluginDir)
        {
            var dbBoot = new DatabaseBootstrapper(pluginDir, _logger);
            dbBoot.Initialize();
            dbBoot.PruneLockFile(); 
        }

        private void InitializeChannels()
        {
            _identityChannel = new IdentityChannel();
            _routerChannel = new RouterChannel();
            _knowledgeChannel = new KnowledgeChannel();
            _commandChannel = new CommandChannel();
            _outputChannel = new OutputChannel();
            _persistenceChannel = new ChatPersistenceChannel();
        }

        private void InitializeServices(string dbFile)
        {
            _cache = new MemoryCacheService(_logger);
            var buffer = new ChatBufferService(_cache);
            
            var pronounApi = new AlejoPronounService(_logger);
            var pronounRepo = new PronounRepository(_cache, _logger, pronounApi, dbFile);
            var nickRepo = new NicknameRepository(_cache, _logger, dbFile);
            var knowledgeRepo = new KnowledgeRepository(_cache, _logger, dbFile);
            var chatRepo = new TwitchChatRepository(_logger, dbFile);

            _consumers = new List<IConsumer>
            {
                new UserPronounService(_cache, pronounRepo, nickRepo, _logger, _identityChannel, _routerChannel),
                new RouterService(_cache, _logger, _routerChannel, _knowledgeChannel, _commandChannel),
                new UserKnowledgeService(_cache, knowledgeRepo, _logger, _knowledgeChannel, _outputChannel),
                new CommandProcessingService(_cache, nickRepo, knowledgeRepo, _logger, _commandChannel, _outputChannel),
                new TwitchChatMessageService(_cache, chatRepo, _logger, _persistenceChannel),
                new ResponseService(_cache, buffer, _logger, _outputChannel)
            };
        }

        private void StartConsumers()
        {
            foreach (var consumer in _consumers)
            {
                consumer.StartAsync(_globalCts.Token);
            }
        }
    }
}
