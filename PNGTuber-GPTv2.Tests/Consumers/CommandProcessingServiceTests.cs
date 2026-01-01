using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;
using PNGTuber_GPTv2.Consumers.Command;
using PNGTuber_GPTv2.Core.Interfaces;
using PNGTuber_GPTv2.Domain.DTOs;
using PNGTuber_GPTv2.Domain.Entities;
using PNGTuber_GPTv2.Core;
using PNGTuber_GPTv2.Crypto.Channels;

namespace PNGTuber_GPTv2.Tests.Consumers
{
    public class CommandProcessingServiceTests : IDisposable
    {
        private readonly Mock<ICacheService> _mockCache;
        private readonly Mock<INicknameRepository> _mockNickRepo;
        private readonly Mock<IKnowledgeRepository> _mockKnowledge;
        private readonly Mock<ILogger> _mockLogger;
        private readonly CommandChannel _inChannel;
        private readonly OutputChannel _outChannel;
        private readonly CommandProcessingService _service;
        private readonly CancellationTokenSource _cts;

        public CommandProcessingServiceTests()
        {
            _mockCache = new Mock<ICacheService>();
            _mockNickRepo = new Mock<INicknameRepository>();
            _mockKnowledge = new Mock<IKnowledgeRepository>();
            _mockLogger = new Mock<ILogger>();
            
            _inChannel = new CommandChannel();
            _outChannel = new OutputChannel();
            
            _service = new CommandProcessingService(
                _mockCache.Object, 
                _mockNickRepo.Object, 
                _mockKnowledge.Object, 
                _mockLogger.Object,
                _inChannel,
                _outChannel
            );
            
            _cts = new CancellationTokenSource();
            _service.StartAsync(_cts.Token);
        }

        public void Dispose()
        {
            _cts.Cancel();
        }

        [Fact]
        public async Task Process_SetsNickname_WhenCommandIsSetNick()
        {
            // Arrange
            var userId = "twitch:123";
            var context = new RequestContext 
            { 
                EventType = "Chat",
                CleanedMessage = "!setnick Rapid",
                User = new User { Id = userId, DisplayName = "User" }
            };

            _mockCache.Setup(c => c.Get<RequestContext>("req_ctx1")).Returns(context);

            // Act
            _inChannel.TryWrite("ctx1");

            // Assert: Wait for Output Channel write
            await WaitForOutputAsync("ctx1");
            
            _mockNickRepo.Verify(r => r.SetNicknameAsync(userId, "Rapid", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Process_RemovesNickname_WhenCommandIsRemoveNick()
        {
            var userId = "twitch:123";
            var context = new RequestContext 
            { 
                EventType = "Chat",
                CleanedMessage = "!removenick",
                User = new User { Id = userId, DisplayName = "User" }
            };

            _mockCache.Setup(c => c.Get<RequestContext>("req_ctx1")).Returns(context);

            _inChannel.TryWrite("ctx1");
            await WaitForOutputAsync("ctx1");

            _mockNickRepo.Verify(r => r.SetNicknameAsync(userId, null, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Process_UsesGuid_WhenCommandIdIsSet()
        {
            var userId = "twitch:123";
            var context = new RequestContext 
            { 
                EventType = "Command",
                CommandId = CommandRegistry.SetNick,
                CleanedMessage = "!setnick CoolNick", // Used for args
                User = new User { Id = userId, DisplayName = "User" }
            };

            _mockCache.Setup(c => c.Get<RequestContext>("req_ctx1")).Returns(context);

            _inChannel.TryWrite("ctx1");
            await WaitForOutputAsync("ctx1");

            _mockNickRepo.Verify(r => r.SetNicknameAsync(userId, "CoolNick", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Process_HandlesNewCommands()
        {
             var userId = "twitch:123";
             
             // 1. Help
             var ctxHelp = new RequestContext { EventType="Chat", CleanedMessage="!help", User=new User{Id=userId} };
             _mockCache.Setup(c => c.Get<RequestContext>("req_help")).Returns(ctxHelp);
             _inChannel.TryWrite("help");
             await WaitForOutputAsync("help");
             Assert.Contains("!version", ctxHelp.GeneratedResponse);

             // 2. Version
             var ctxVer = new RequestContext { EventType="Chat", CleanedMessage="!version", User=new User{Id=userId} };
             _mockCache.Setup(c => c.Get<RequestContext>("req_ver")).Returns(ctxVer);
             _inChannel.TryWrite("ver");
             await WaitForOutputAsync("ver");
             Assert.Contains("v2.0.0", ctxVer.GeneratedResponse);

             // 3. SetPronouns
             var ctxPro = new RequestContext { EventType="Chat", CleanedMessage="!setpronouns", User=new User{Id=userId} };
             _mockCache.Setup(c => c.Get<RequestContext>("req_pro")).Returns(ctxPro);
             _inChannel.TryWrite("pro");
             await WaitForOutputAsync("pro");
             Assert.Contains("alejo.io", ctxPro.GeneratedResponse);
        }
        
        private async Task WaitForOutputAsync(string expectedId)
        {
            // Simple wait loop
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            while (await _outChannel.WaitToReadAsync(cts.Token))
            {
                if (_outChannel.TryRead(out var id) && id == expectedId) return;
            }
        }
    }
}
