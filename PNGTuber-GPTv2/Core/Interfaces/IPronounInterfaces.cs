using System.Threading;
using System.Threading.Tasks;
using PNGTuber_GPTv2.Domain.Structs;

namespace PNGTuber_GPTv2.Core.Interfaces
{
    public interface IPronounService
    {
        Task<Pronouns?> FetchPronounsAsync(string platformId, CancellationToken ct);
    }

    public interface IPronounRepository
    {
        Task<Pronouns> GetPronounsAsync(string userId, string displayName, CancellationToken ct);
    }
}
