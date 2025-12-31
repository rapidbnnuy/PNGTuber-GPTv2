using System;
using System.Collections.Generic;
using Streamer.bot.Plugin.Interface;
using PNGTuber_GPTv2.Core;
using PNGTuber_GPTv2.Core.Interfaces;

namespace PNGTuber_GPTv2
{
    public class CPHInline
    {
        public IInlineInvokeProxy CPH { get; set; }

        private static BotEngine _engine;
        private static readonly object _lock = new object();

        // ------------------------------------------------------------
        // Adapter Implementation
        // ------------------------------------------------------------
        public class CPHAdapter : IStreamerBotProxy
        {
            private readonly IInlineInvokeProxy _cph;

            public CPHAdapter(IInlineInvokeProxy cph)
            {
                _cph = cph;
            }

            public T GetGlobalVar<T>(string varName, bool persisted = true)
            {
                return _cph.GetGlobalVar<T>(varName, persisted);
            }

            public bool TryGetArg<T>(string argName, out T value)
            {
                return _cph.TryGetArg<T>(argName, out value);
            }

            public void LogInfo(string message)
            {
                _cph.LogInfo(message);
            }

            public void LogError(string message, Exception ex)
            {
                // CPH LogInfo fallback for now as LogError might not exist in older interfaces
                _cph.LogInfo($"[ERROR] {message} - {ex}");
            }
        }
        // ------------------------------------------------------------

        public bool Execute()
        {
            lock (_lock)
            {
                if (_engine == null)
                {
                    var adapter = new CPHAdapter(CPH);
                    _engine = new BotEngine(adapter);
                    if (!_engine.Start())
                    {
                        _engine = null;
                        return false;
                    }
                }
            }

            // Extract Args using CPH directly (legacy) or Adapter?
            // Existing logic uses local TryAddVar helper.
            // Let's keep existing logic for now to minimize diff, but BotEngine only uses Adapter.
            
            var eventArgs = new Dictionary<string, object>();
            
            TryAddVar(eventArgs, "commandId");
            TryAddVar(eventArgs, "command");
            TryAddVar(eventArgs, "triggerType");
            
            TryAddVar(eventArgs, "user");
            TryAddVar(eventArgs, "userName");
            TryAddVar(eventArgs, "userId"); 
            TryAddVar(eventArgs, "display_name");
            TryAddVar(eventArgs, "message");
            TryAddVar(eventArgs, "rawInput");

            _engine.Ingest(eventArgs);
            return true;
        }

        private void TryAddVar(Dictionary<string, object> dict, string key)
        {
            try 
            {
                if (CPH.TryGetArg<object>(key, out var val)) dict[key] = val;
            }
            catch {}
        }
        
        public bool Shutdown()
        {
             lock (_lock)
             {
                 _engine?.Shutdown();
                 _engine = null;
             }
             return true;
        }
    }
}
