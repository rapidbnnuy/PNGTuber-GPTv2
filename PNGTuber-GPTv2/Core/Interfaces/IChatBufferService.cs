using System.Collections.Generic;

namespace PNGTuber_GPTv2.Core.Interfaces
{
    public interface IChatBufferService
    {
        void AddMessage(string formattedMessage);
        List<string> GetRecentMessages();
    }
}
