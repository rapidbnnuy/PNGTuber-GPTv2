using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Moq;
using PNGTuber_GPTv2.Core.Interfaces;
using PNGTuber_GPTv2.Infrastructure.Persistence;
using PNGTuber_GPTv2.Infrastructure.Caching;

namespace PNGTuber_GPTv2.Tests.Integration
{
    [Collection("Serial Integration Tests")]
    public class KnowledgeRepositoryTests : IDisposable
    {
        private readonly string _dbPath;
        private readonly KnowledgeRepository _repo;
        private readonly Mock<ILogger> _mockLogger;
        private readonly Mock<ICacheService> _mockCache;

        public KnowledgeRepositoryTests()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), $"test_knowledge_{Guid.NewGuid()}.db");
            _mockLogger = new Mock<ILogger>();
            _mockCache = new Mock<ICacheService>();
            
            // Note: Cache is not really used for Knowledge yet, but required by constructor if we add it.
            // For now, Repo constructor might just take Logger and DB Path.
            
            _repo = new KnowledgeRepository(_mockCache.Object, _mockLogger.Object, _dbPath);
        }

        [Fact]
        public async Task AddAndSearch_PersistsFact()
        {
            var ct = CancellationToken.None;
            await _repo.AddFactAsync("rust", "Rust is a systems programming language.", "user1", ct);

            var results = await _repo.SearchFactsAsync("rust", ct);
            
            Assert.Single(results);
            Assert.Equal("rust", results[0].Key);
            Assert.Equal("Rust is a systems programming language.", results[0].Content);
            
            // Verify Cache Write for "knowledge_all" (invalidation) and specific fact
            _mockCache.Verify(c => c.Remove("knowledge_all"), Times.Once);
            _mockCache.Verify(c => c.Set("fact_rust", "Rust is a systems programming language.", It.IsAny<TimeSpan>()), Times.Once);
        }

        [Fact]
        public async Task Search_IsCaseInsensitive()
        {
            var ct = CancellationToken.None;
            await _repo.AddFactAsync("Godot", "Game Engine", "user1", ct);

            var results = await _repo.SearchFactsAsync("godot", ct);
            Assert.Single(results);
        }

        [Fact]
        public async Task Remove_DeletesFact()
        {
            var ct = CancellationToken.None;
            await _repo.AddFactAsync("temp", "delete me", "user1", ct);
            
            await _repo.RemoveFactAsync("temp", ct);
            
            var results = await _repo.SearchFactsAsync("temp", ct);
            Assert.Empty(results);
        }

        public void Dispose()
        {
            try { if (File.Exists(_dbPath)) File.Delete(_dbPath); } catch {}
        }
    }
}
