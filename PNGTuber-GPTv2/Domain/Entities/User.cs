using System;

namespace PNGTuber_GPTv2.Domain.Entities
{
    public class User
    {
        public string Id { get; set; } 
        public string DisplayName { get; set; }
        public string Nickname { get; set; }
        public string Platform { get; set; }
        public int Karma { get; set; }
        public DateTime FirstSeen { get; set; }
        public DateTime LastSeen { get; set; }
    }
}
