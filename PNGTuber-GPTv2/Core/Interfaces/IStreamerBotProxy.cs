using System;

namespace PNGTuber_GPTv2.Core.Interfaces
{
    // Adapter Interface to decouple from concrete Streamer.bot DLLs during testing
    public interface IStreamerBotProxy
    {
        T GetGlobalVar<T>(string varName, bool persisted = true);
        bool TryGetArg<T>(string argName, out T value);
        void LogInfo(string message);
        void LogError(string message, Exception ex);
    }
}
