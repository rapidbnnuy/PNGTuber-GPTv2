using System.Threading;
using System.Threading.Tasks;

namespace PNGTuber_GPTv2.Core.Interfaces
{
    public interface INicknameRepository
    {
        // returns null if no nickname set
        Task<string> GetNicknameAsync(string userId, CancellationToken ct);
        Task SetNicknameAsync(string userId, string nickname, CancellationToken ct);
    }
}
