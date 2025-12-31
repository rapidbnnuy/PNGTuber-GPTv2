using System;

namespace PNGTuber_GPTv2.Domain.Structs
{
    public readonly struct ChatMessage
    {
        public string Id { get; }
        public string UserId { get; }
        public string Platform { get; }
        public string Message { get; }
        public string DisplayName { get; }
        public int Role { get; } // 1=Broadcaster, 2=Mod, 3=VIP, 4=User
        public bool IsSub { get; }
        public Pronouns Pronouns { get; } // Injected/Cached

        public ChatMessage(string id, string userId, string platform, string message, string displayName, int role, bool isSub, Pronouns pronouns)
        {
            Id = id;
            UserId = userId;
            Platform = platform;
            Message = message;
            DisplayName = displayName;
            Role = role;
            IsSub = isSub;
            Pronouns = pronouns;
        }
    }
}
