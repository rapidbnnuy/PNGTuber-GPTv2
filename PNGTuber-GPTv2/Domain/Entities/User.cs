using System;

namespace PNGTuber_GPTv2.Domain.Entities
{
    public class User
    {
        public string Id { get; set; } // "twitch:12345"
        public string DisplayName { get; set; }
        public string Nickname { get; set; } // Custom override
        public string Platform { get; set; }
        public int Karma { get; set; }
        public DateTime FirstSeen { get; set; }
        public DateTime LastSeen { get; set; }
    }
}
