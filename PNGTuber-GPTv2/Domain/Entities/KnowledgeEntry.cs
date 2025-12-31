using System;

namespace PNGTuber_GPTv2.Domain.Entities
{
    public class KnowledgeEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Key { get; set; }
        public string Content { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
