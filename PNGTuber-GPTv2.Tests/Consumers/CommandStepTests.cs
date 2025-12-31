using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;
using PNGTuber_GPTv2.Consumers.Command;
using PNGTuber_GPTv2.Core.Interfaces;
using PNGTuber_GPTv2.Domain.DTOs;
using PNGTuber_GPTv2.Domain.Entities;

namespace PNGTuber_GPTv2.Tests.Consumers
{
    public class CommandStepTests
    {
        private readonly Mock<ICacheService> _mockCache;
        private readonly Mock<INicknameRepository> _mockNickRepo;
        private readonly Mock<IKnowledgeRepository> _mockKnowledge;
        private readonly Mock<ILogger> _mockLogger;
        private readonly CommandStep _step;

        public CommandStepTests()
        {
            _mockCache = new Mock<ICacheService>();
            _mockNickRepo = new Mock<INicknameRepository>();
            _mockKnowledge = new Mock<IKnowledgeRepository>();
            _mockLogger = new Mock<ILogger>();
            _step = new CommandStep(_mockCache.Object, _mockNickRepo.Object, _mockKnowledge.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task ExecuteAsync_SetsNickname_WhenCommandIsSetNick()
        {
            // Arrange
            var userId = "twitch:123";
            var context = new RequestContext 
            { 
                EventType = "Chat",
                CleanedMessage = "!setnick Rapid",
                User = new User { Id = userId, DisplayName = "User" }
            };

            _mockCache.Setup(c => c.Get<RequestContext>(It.IsAny<string>()))
                      .Returns(context);

            // Act
            await _step.ExecuteAsync("ctx1", CancellationToken.None);

            // Assert
            _mockNickRepo.Verify(r => r.SetNicknameAsync(userId, "Rapid", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_RemovesNickname_WhenCommandIsRemoveNick()
        {
            // Arrange
            var userId = "twitch:123";
            var context = new RequestContext 
            { 
                EventType = "Chat",
                CleanedMessage = "!removenick",
                User = new User { Id = userId, DisplayName = "User" }
            };

            _mockCache.Setup(c => c.Get<RequestContext>(It.IsAny<string>()))
                      .Returns(context);

            // Act
            await _step.ExecuteAsync("ctx1", CancellationToken.None);

            // Assert
            _mockNickRepo.Verify(r => r.SetNicknameAsync(userId, null, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_DoesNothing_WhenNicknameTooLong()
        {
            // Arrange
            var userId = "twitch:123";
            var longNick = new string('a', 31);
            var context = new RequestContext 
            { 
                EventType = "Chat",
                CleanedMessage = $"!setnick {longNick}",
                User = new User { Id = userId, DisplayName = "User" }
            };

            _mockCache.Setup(c => c.Get<RequestContext>(It.IsAny<string>()))
                      .Returns(context);

            // Act
            await _step.ExecuteAsync("ctx1", CancellationToken.None);

            // Assert
            _mockNickRepo.Verify(r => r.SetNicknameAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
