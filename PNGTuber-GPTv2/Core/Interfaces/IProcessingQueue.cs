using System.Threading;
using System.Threading.Tasks;

namespace PNGTuber_GPTv2.Core.Interfaces
{
    public interface IProcessingQueue
    {
        bool TryEnqueue(string contextId);
        ValueTask<bool> WaitToReadAsync(CancellationToken ct);
        bool TryDequeue(out string contextId);
    }
}
