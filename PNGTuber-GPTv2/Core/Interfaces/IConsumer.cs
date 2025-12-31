using System.Threading.Tasks;
using System.Threading;

namespace PNGTuber_GPTv2.Core.Interfaces
{
    public interface IConsumer<in T>
    {
        Task ProcessAsync(T message, CancellationToken ct);
    }
}
