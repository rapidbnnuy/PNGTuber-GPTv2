using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PNGTuber_GPTv2.Domain.Entities;

namespace PNGTuber_GPTv2.Core.Interfaces
{
    public interface IKnowledgeRepository
    {
        Task AddFactAsync(string key, string content, string userId, CancellationToken ct);
        Task RemoveFactAsync(string key, CancellationToken ct);
        Task<List<KnowledgeEntry>> SearchFactsAsync(string query, CancellationToken ct);
        Task<List<KnowledgeEntry>> GetAllFactsAsync(CancellationToken ct);
    }
}
