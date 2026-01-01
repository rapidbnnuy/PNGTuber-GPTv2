using System;

namespace PNGTuber_GPTv2.Domain.Entities
{
    public class ChatMessage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string UserId { get; set; }
        public string DisplayName { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
        public string FullText { get; set; }
    }
}
