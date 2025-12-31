using System;
using System.Collections.Generic;
using PNGTuber_GPTv2.Domain.Entities;
using PNGTuber_GPTv2.Domain.Structs;

namespace PNGTuber_GPTv2.Domain.DTOs
{

    public class RequestContext
    {
        public string RequestId { get; set; }
        public DateTime CreatedAt { get; set; }
        
        public Dictionary<string, object> RawArgs { get; set; }

        public string EventType { get; set; }
        public string CommandId { get; set; }

        public User User { get; set; }
        public Pronouns Pronouns { get; set; }

        public string CleanedMessage { get; set; }
        public bool IsModerationFlagged { get; set; }
        public string ModerationReason { get; set; }

        public string GeneratedResponse { get; set; }

        public RequestContext()
        {
            RequestId = Guid.NewGuid().ToString("N");
            CreatedAt = DateTime.UtcNow;
            RawArgs = new Dictionary<string, object>();
        }
    }
}
