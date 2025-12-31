using System;
using System.Collections.Generic;
using System.Threading.Channels;
using LiteDB;
using Streamer.bot.Plugin.Interface;

namespace PNGTuber_GPTv2
{
    public class CPHInline
    {
        public IInlineInvokeProxy CPH { get; set; }
        private PNGTuber_GPTv2.Core.Interfaces.ILogger _logger;

        public bool Execute()
        {
            // 1. Resolve Configuration
            string databasePathVar = CPH.GetGlobalVar<string>("Database Path", true);
            string pluginBaseDir = PNGTuber_GPTv2.Infrastructure.FileSystem.Bootstrapper.Initialize(databasePathVar);

            string globalLogLevelStr = CPH.GetGlobalVar<string>("Logging Level", true) ?? "INFO";

            // 2. Parse Log Level
            if (!Enum.TryParse(globalLogLevelStr, true, out PNGTuber_GPTv2.Domain.Enums.LogLevel configuredLevel))
            {
                configuredLevel = PNGTuber_GPTv2.Domain.Enums.LogLevel.Info;
                CPH.LogInfo($"Invalid Global Var 'Logging Level': {globalLogLevelStr}. Defaulting to INFO.");
            }

            // 3. Initialize Logger
            // Bootstrapper guarantees pluginBaseDir exists. Logger now uses it as root.
            // Note: FileLogger was expecting 'basePath' and appended 'PNGTuber-GPT'. 
            // Since Bootstrapper returns the FULL path (%Install%/PNGTuber-GPT), we need to adjust Logger or pass parent.
            // Let's adjust logger usage. If we pass pluginBaseDir to Logger, logic inside Logger needs to be clean.
            // Currently FileLogger: _logDirectory = Path.Combine(basePath, "PNGTuber-GPT", "logs");
            // If we pass pluginBaseDir, it will be %Install%/PNGTuber-GPT/PNGTuber-GPT/logs. That is wrong.
            // So we pass 'databasePathVar' (or installDir) to FileLogger if we keep it as is.
            // OR we fix FileLogger to take the PluginDir directly. 
            // Let's pass the 'databasePathVar' (raw install dir) to Logger for now to verify.
            
            _logger = new PNGTuber_GPTv2.Infrastructure.Logging.FileLogger(databasePathVar, configuredLevel);

            _logger.Info("PNGTuber-GPTv2 Plugin Initialized.");
            
            // Verify System.Threading.Channels
            var channel = System.Threading.Channels.Channel.CreateUnbounded<string>();
            _logger.Debug($"Channel created with capacity: {channel.Reader.CanCount}");

            // Verify LiteDB
            try 
            {
                using(var db = new LiteDB.LiteDatabase(":memory:"))
                {
                    var col = db.GetCollection<LiteDB.BsonDocument>("test");
                    col.Insert(new LiteDB.BsonDocument { ["msg"] = "LiteDB Works!" });
                    _logger.Debug($"LiteDB Insert count: {col.Count()}");
                }
            }
            catch(Exception ex)
            {
                _logger.Error($"LiteDB Error: {ex.Message}");
            }

            return true;
        }
    }
}
