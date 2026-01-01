using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;
using PNGTuber_GPTv2.Core.Interfaces;
using PNGTuber_GPTv2.Infrastructure.Persistence;

namespace PNGTuber_GPTv2.Tests.Integration
{
    [Collection("Serial Integration Tests")]
    public class NicknameRepositoryTests : IDisposable
    {
        private readonly string _tempDb;
        private readonly Mock<ICacheService> _mockCache;
        private readonly Mock<ILogger> _mockLogger;
        private readonly NicknameRepository _repo;

        public NicknameRepositoryTests()
        {
            Environment.SetEnvironmentVariable("PNGTUBER_TEST_MUTEX", $"TestMutex_{Guid.NewGuid():N}");
            _tempDb = Path.Combine(Path.GetTempPath(), $"test_db_{Guid.NewGuid()}.db");
            _mockCache = new Mock<ICacheService>();
            _mockLogger = new Mock<ILogger>();
            
            // Allow cache miss
            _mockCache.Setup(c => c.Exists(It.IsAny<string>())).Returns(false);

            _repo = new NicknameRepository(_mockCache.Object, _mockLogger.Object, _tempDb);
        }

        [Fact]
        public async Task SetAndGet_PersistsToDisk()
        {
            var userId = "twitch:123";
            var nick = "TestNick";

            await _repo.SetNicknameAsync(userId, nick, CancellationToken.None);

            // Re-instantiate repo to simulate app restart (though simple object reuse is fine for LiteDB as it re-opens connection)
            // But we specifically want to verify it reads from DISK, not cache (Mock checks that)
            // Since we mocked Cache to return false (Miss), it MUST force a DB read.
            
            var result = await _repo.GetNicknameAsync(userId, CancellationToken.None);
            
            Assert.Equal(nick, result);
        }
        
        [Fact]
        public async Task SetToNull_RemovesNickname()
        {
            var userId = "twitch:456";
            await _repo.SetNicknameAsync(userId, "Original", CancellationToken.None);
            await _repo.SetNicknameAsync(userId, null, CancellationToken.None);
            
            var result = await _repo.GetNicknameAsync(userId, CancellationToken.None);
            
            Assert.Null(result);
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable("PNGTUBER_TEST_MUTEX", null);
            if (File.Exists(_tempDb))
            {
                try { File.Delete(_tempDb); } catch {}
            }
        }
    }
}
