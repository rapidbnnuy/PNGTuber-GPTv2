using System.Collections.Generic;
using Xunit;
using Moq;
using PNGTuber_GPTv2.Core.Interfaces;
using PNGTuber_GPTv2.Infrastructure.Services;

namespace PNGTuber_GPTv2.Tests.Infrastructure
{
    public class ChatHistoryServiceTests
    {
        private readonly Mock<ICacheService> _mockCache;
        private readonly ChatHistoryService _service;

        public ChatHistoryServiceTests()
        {
            _mockCache = new Mock<ICacheService>();
            _service = new ChatHistoryService(_mockCache.Object);
        }

        [Fact]
        public void AddMessage_CapsAt20()
        {
            for (int i = 0; i < 25; i++)
            {
                _service.AddMessage($"Msg {i}");
            }

            var recent = _service.GetRecentMessages();
            
            Assert.Equal(20, recent.Count);
            Assert.Equal("Msg 5", recent[0]); // 0-4 should be gone
            Assert.Equal("Msg 24", recent[19]);
        }
        
        [Fact]
        public void AddMessage_UpdatesCache()
        {
            _service.AddMessage("Hello");
            
            _mockCache.Verify(c => c.Set("chat_history", It.IsAny<List<string>>(), It.IsAny<System.TimeSpan>()), Times.Once);
        }
    }
}
