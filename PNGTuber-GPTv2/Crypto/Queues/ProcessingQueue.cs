using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using PNGTuber_GPTv2.Core.Interfaces;

namespace PNGTuber_GPTv2.Crypto.Queues
{
    public class ProcessingQueue : IProcessingQueue
    {
        private readonly Channel<string> _channel;

        public ProcessingQueue()
        {
            _channel = Channel.CreateUnbounded<string>();
        }

        public bool TryEnqueue(string contextId)
        {
            return _channel.Writer.TryWrite(contextId);
        }

        public ValueTask<bool> WaitToReadAsync(CancellationToken ct)
        {
            return _channel.Reader.WaitToReadAsync(ct);
        }

        public bool TryDequeue(out string contextId)
        {
            return _channel.Reader.TryRead(out contextId);
        }
    }
}
