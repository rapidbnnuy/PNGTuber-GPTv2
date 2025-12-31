using System.Threading.Tasks;
using System.Threading;

namespace PNGTuber_GPTv2.Core.Interfaces
{
    public interface IEventPublisher
    {
        Task PublishAsync<T>(T message, CancellationToken ct = default);
    }
}
