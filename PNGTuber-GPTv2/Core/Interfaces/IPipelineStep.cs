using System.Threading.Tasks;
using System.Threading;
using PNGTuber_GPTv2.Core.Interfaces;

namespace PNGTuber_GPTv2.Core.Interfaces
{
    public interface IPipelineStep
    {
        Task ExecuteAsync(string contextId, CancellationToken ct);
    }
}
