using System.Threading.Tasks;
using System.Threading;

namespace PNGTuber_GPTv2.Core.Interfaces
{
    public interface IConsumer
    {
        Task StartAsync(CancellationToken ct);
    }
}
