using System.Threading.Tasks;
using System.Threading;
using PNGTuber_GPTv2.Core.Interfaces;

namespace PNGTuber_GPTv2.Core.Interfaces
{
    public interface IPipelineStep
    {
        // Executes a logical block of work on the Context.
        // It fetches the context from Cache using contextId, mutates it, and saves it back.
        Task ExecuteAsync(string contextId, CancellationToken ct);
    }
}
