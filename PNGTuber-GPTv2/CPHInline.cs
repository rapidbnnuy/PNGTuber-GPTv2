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
            string globalLogLevelStr = CPH.GetGlobalVar<string>("Logging Level", true) ?? "INFO";
            string databasePath = CPH.GetGlobalVar<string>("Database Path", true) ?? "."; // Fallback to current dir

            // 2. Parse Log Level
            if (!Enum.TryParse(globalLogLevelStr, true, out PNGTuber_GPTv2.Domain.Enums.LogLevel configuredLevel))
            {
                configuredLevel = PNGTuber_GPTv2.Domain.Enums.LogLevel.Info;
                CPH.LogInfo($"Invalid Global Var 'Logging Level': {globalLogLevelStr}. Defaulting to INFO.");
            }

            // 3. Initialize Logger
            _logger = new PNGTuber_GPTv2.Infrastructure.Logging.FileLogger(databasePath, configuredLevel);

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
