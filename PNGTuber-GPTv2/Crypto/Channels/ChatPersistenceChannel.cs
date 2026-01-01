using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using PNGTuber_GPTv2.Core.Interfaces;

namespace PNGTuber_GPTv2.Crypto.Channels
{
    public class ChatPersistenceChannel : IProcessingChannel<string>
    {
        private readonly Channel<string> _channel;

        public ChatPersistenceChannel()
        {
            _channel = Channel.CreateUnbounded<string>();
        }

        public bool TryWrite(string contextId)
        {
            return _channel.Writer.TryWrite(contextId);
        }

        public ValueTask<bool> WaitToReadAsync(CancellationToken ct)
        {
            return _channel.Reader.WaitToReadAsync(ct);
        }

        public bool TryRead(out string contextId)
        {
            return _channel.Reader.TryRead(out contextId);
        }
    }
}
