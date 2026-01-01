using System;

namespace PNGTuber_GPTv2.Core.Interfaces
{
    public interface IStreamerBotProxy
    {
        T GetGlobalVar<T>(string varName, bool persisted = true);
        bool UnsetGlobalVar(string varName, bool persisted = true);

        void EnableCommand(string id);
        void DisableCommand(string id);
        bool TryGetArg<T>(string argName, out T value);
        void LogInfo(string message);
        void LogError(string message, Exception ex);
    }
}
