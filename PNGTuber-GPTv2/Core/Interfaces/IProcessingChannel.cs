using System.Threading;
using System.Threading.Tasks;

namespace PNGTuber_GPTv2.Core.Interfaces
{
    public interface IProcessingChannel<T>
    {
        bool TryWrite(T item);
        ValueTask<bool> WaitToReadAsync(CancellationToken ct);
        bool TryRead(out T item);
    }
}
