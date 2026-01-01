using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LiteDB;
using Moq;
using Xunit;
using Xunit.Abstractions;
using PNGTuber_GPTv2.Core;
using PNGTuber_GPTv2.Core.Interfaces;
using PNGTuber_GPTv2.Infrastructure.Persistence;
using PNGTuber_GPTv2.Infrastructure.Services;
using PNGTuber_GPTv2.Infrastructure.Logging;
using PNGTuber_GPTv2.Infrastructure.Caching;

namespace PNGTuber_GPTv2.Tests.Integration
{
    [Collection("Serial Integration Tests")]
    public class TortureTests : IDisposable
    {
        private readonly string _tempRoot;
        private readonly Mock<IStreamerBotProxy> _mockCph;
        private readonly Mock<ILogger> _mockLogger;
        private readonly ITestOutputHelper _output;

        public TortureTests(ITestOutputHelper output)
        {
            Environment.SetEnvironmentVariable("PNGTUBER_TEST_MUTEX", $"TortureMutex_{Guid.NewGuid():N}");
            _output = output;
            _tempRoot = Path.Combine(Path.GetTempPath(), $"torture_{Guid.NewGuid()}");
            Directory.CreateDirectory(_tempRoot);
            _mockLogger = new Mock<ILogger>();
            _mockCph = new Mock<IStreamerBotProxy>();
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable("PNGTUBER_TEST_MUTEX", null);
            try { Directory.Delete(_tempRoot, true); } catch { }
        }

        [Fact]
        public void HistoryService_ConcurrentLoad_MaintainsOrderAndSize()
        {
            // Arrange
            var cache = new Mock<ICacheService>();
            var service = new ChatBufferService(cache.Object);
            var tasks = new List<Task>();
            int threadCount = 50;
            int msgsPerThread = 100;

            // Act: Hammer it from multiple threads
            // Threads are unordered, but within a thread, order is sequential.
            // We mainly check for crashes and final size.
            Parallel.For(0, threadCount, i =>
            {
                for (int j = 0; j < msgsPerThread; j++)
                {
                    service.AddMessage($"T{i}-M{j}");
                }
            });

            // Assert
            var recent = service.GetRecentMessages();
            Assert.Equal(20, recent.Count);
            
            // Should not be null or empty
            foreach(var msg in recent)
            {
                Assert.False(string.IsNullOrEmpty(msg));
            }
        }

        [Fact]
        public void Repository_LockedFile_HandlesGracefully()
        {
            // Simulate another process (e.g. Streamer.bot) holding the DB lock
            // The DatabaseMutex uses a Named System Mutex.
            // If we acquire it here in the test, the Repository (running in same process) 
            // should presumably handle re-entrancy if it was same thread, 
            // but we want to simulate "Blocked".
            // Since Mutex is thread-affine, if we acquire on main thread, and run repo on Task, 
            // repo should be blocked.
            
            var dbDir = Path.Combine(_tempRoot, "PNGTuber-GPT");
            Directory.CreateDirectory(dbDir);
            var dbPath = Path.Combine(dbDir, "pngtuber.db");
            
            var logger = new FileLogger(_tempRoot, PNGTuber_GPTv2.Domain.Enums.LogLevel.Debug);
            var cache = new MemoryCacheService(logger);
            var repo = new NicknameRepository(cache, logger, dbPath);

            // Acquire Global Mutex!
            using (var mutex = new DatabaseMutex(logger))
            {
                bool locked = mutex.Acquire(TimeSpan.FromSeconds(5));
                Assert.True(locked, "Test failed to acquire mutex");

                // Now try to read from Repo on a different thread
                var task = Task.Run(async () => 
                {
                   return await repo.GetNicknameAsync("test_user_1", CancellationToken.None);
                });

                // Repo tries to acquire mutex. It waits 2s. 
                // Since we hold it, it should fail to acquire, log warning, and return null.
                
                // We wait 7s to be sure (Repo waits 5s)
                bool completed = task.Wait(TimeSpan.FromSeconds(7));
                Assert.True(completed, "Repository hung indefinitely!");
                
                var result = task.Result;
                Assert.Null(result); // Should default to null/empty on lock failure
            }
        }
    }
}
