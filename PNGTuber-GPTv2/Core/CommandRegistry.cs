using System.Collections.Generic;
using PNGTuber_GPTv2.Core.Interfaces;

namespace PNGTuber_GPTv2.Core
{
    public static class CommandRegistry
    {
        public const string Help = "c2332078-d9cb-4cbc-80ce-21bb86330986";
        public const string ClearChatHistory = "8ac3c57a-d353-4bdb-8768-52eb15352a4a";
        public const string ClearPromptHistory = "799c4402-33f3-425d-979f-0fd36e27a1aa";
        public const string ClearLogFile = "e5759e3f-edc1-453d-a67a-009c013d7e4c";
        public const string Version = "e87f7481-9b19-4a5e-8b43-20516768393e";

        public const string SetNick = "ccd7aa20-4a65-4467-ad34-4a50ff74e163";
        public const string RemoveNick = "72a08e87-463b-4a3d-ae16-de5b8f51c18f";
        public const string CurrentNick = "17b06f6a-2f25-4fd0-a0e6-48aacf5cc782";
        public const string SetPronouns = "777782d6-9e97-4dae-9f86-dafaa58cafaa";
        public const string Forget = "23890ed1-131a-42bd-90f0-9d36c5b775b6";
        public const string GetMemory = "23a7aa90-a1ad-464c-a6c4-861f73223084";
        public const string RememberThis = "80f22f3f-0197-4347-b37f-b6993c86d68a";
        public const string ForgetThis = "66cdc4d3-c52d-4c31-b323-277f4d52c6a0";

        public const string GptChat1 = "6a23a150-93ce-4201-8db2-d2fd866f90d8";
        public const string GptChat2 = "43cd2165-35d8-4250-809a-91ea076c9b9f";
        public const string GptChat3 = "b2167b8a-d9ab-41d2-bdf2-5a17138316f6";
        public const string GptChat4 = "c2108c71-e7b1-4f66-8481-56c9fb20ab2a";
        public const string GptChat5 = "58528480-483f-4612-b172-eae7f341ea0a";

        public const string SayPlay = "6caffb4d-7a47-46cc-b7c2-084cd49127b6";
        public const string Speak1 = "1b833436-5215-4053-a8e3-46d9f1ba3af7";
        public const string Speak2 = "38f9b8a5-8aed-42e8-916d-effd433777bb";
        public const string Speak3 = "35b592dc-c861-449e-861f-93abbf203ef3";
        public const string Speak4 = "90ab0841-9de0-4579-bcc9-a65ac253d1a1";
        public const string Speak5 = "3ef90757-e83d-4f7b-9a54-d8a4edd7e1e8";

        private static readonly Dictionary<string, string> _names = new Dictionary<string, string>
        {
            { Help, "!help" },
            { ClearChatHistory, "!clearchathistory" },
            { ClearPromptHistory, "!clearprompthistory" },
            { ClearLogFile, "!clearlogfile" },
            { Version, "!version" },
            { SetNick, "!setnick" },
            { RemoveNick, "!removenick" },
            { CurrentNick, "!currentnick" },
            { SetPronouns, "!setpronouns" },
            { Forget, "!forget" },
            { GetMemory, "!getmemory" },
            { RememberThis, "!rememberthis" },
            { ForgetThis, "!forgetthis" },
            { GptChat1, "!? <msg>" },
            { GptChat2, "!? 2 <msg>" },
            { GptChat3, "!? 3 <msg>" },
            { GptChat4, "!? 4 <msg>" },
            { GptChat5, "!? 5 <msg>" },
            { SayPlay, "!sayplay" },
            { Speak1, "!speak" },
            { Speak2, "!speak 2" },
            { Speak3, "!speak 3" },
            { Speak4, "!speak 4" },
            { Speak5, "!speak 5" }
        };

        public static void RegisterAll(IStreamerBotProxy cph)
        {
            foreach (var kvp in _names)
            {
                cph.EnableCommand(kvp.Key);
            }
        }

        public static string GetName(string id)
        {
            return _names.ContainsKey(id) ? _names[id] : "Unknown";
        }
    }
}
