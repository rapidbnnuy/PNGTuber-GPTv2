using System.Collections.Generic;
using System.Threading.Tasks;
using PNGTuber_GPTv2.Domain.Entities;

namespace PNGTuber_GPTv2.Core.Interfaces
{
    public interface IChatMessageRepository
    {
        Task AddAsync(ChatMessage message);
        Task<List<ChatMessage>> GetRecentAsync(int count);
    }
}
