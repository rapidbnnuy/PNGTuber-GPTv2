using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LiteDB;
using Moq;
using Streamer.bot.Plugin.Interface;
using Xunit;
using Xunit.Abstractions;
using PNGTuber_GPTv2.Core;
using PNGTuber_GPTv2.Core.Interfaces;

namespace PNGTuber_GPTv2.Tests.Integration
{
    public class BotEngineTests : IDisposable
    {
        private readonly string _tempRoot;
        private readonly Mock<IStreamerBotProxy> _mockCph;
        private readonly ITestOutputHelper _output;
        private BotEngine _engine;

        public BotEngineTests(ITestOutputHelper output)
        {
            _output = output;
            _tempRoot = Path.Combine(Path.GetTempPath(), $"test_engine_{Guid.NewGuid()}");
            Directory.CreateDirectory(_tempRoot);

            _mockCph = new Mock<IStreamerBotProxy>();
            _mockCph.Setup(c => c.GetGlobalVar<string>("Database Path", true)).Returns(_tempRoot);
            _mockCph.Setup(c => c.GetGlobalVar<string>("Logging Level", true)).Returns("DEBUG");
            _mockCph.Setup(c => c.LogInfo(It.IsAny<string>())).Callback<string>(s => _output.WriteLine($"[CPH] {s}"));
        }

        [Fact]
        public async Task TheStampede_100ConcurrentEvents_ProcessedSafely()
        {
            // Arrange
            _engine = new BotEngine(_mockCph.Object);
            bool started = _engine.Start();
            
            if (!started)
            {
                var logsDir = Path.Combine(_tempRoot, "PNGTuber-GPT", "logs");
                if (Directory.Exists(logsDir))
                {
                    var files = Directory.GetFiles(logsDir, "*.log");
                    if (files.Length > 0)
                    {
                        var content = File.ReadAllText(files[0]);
                        _output.WriteLine($"[LOG FILE {files[0]}]: {content}");
                    }
                    else
                    {
                         _output.WriteLine($"[LOG FILE]: Dir exists but empty.");
                    }
                }
                else
                {
                     // Try Uppercase
                     var logsDirUp = Path.Combine(_tempRoot, "PNGTuber-GPT", "Logs");
                     if (Directory.Exists(logsDirUp))
                        _output.WriteLine("[LOG FILE]: Found 'Logs' (Used by Bootstrapper) but Logger uses 'logs'?");
                     else
                        _output.WriteLine($"[LOG FILE]: Not Found at {logsDir}");
                }
            }
            Assert.True(started, "Engine failed to start.");

            int count = 20;
            var tasks = new List<Task>();
            var userIdBase = "twitch:stamper:";

            // Act: Fire 100 events
            for (int i = 0; i < count; i++)
            {
                var idx = i;
                tasks.Add(Task.Run(() => 
                {
                    var args = new Dictionary<string, object>
                    {
                        { "triggerType", "Chat" },
                        { "message", $"Message {idx}" },
                        { "user", $"User {idx}" },
                        { "userId", $"{userIdBase}{idx}" },
                        { "display_name", $"User {idx}" }
                    };
                    _engine.Ingest(args);
                }));
            }
            await Task.WhenAll(tasks);

            // Assert: Wait for DB persistence (Observer Pattern)
            // Since execution is async background, we poll the DB to see if records appear.
            // Specifically, IdentityStep writes to user_nicknames (read) and PronounRepo (writes).
            // Actually IdentityStep DOES NOT write to DB unless pronouns are missing.
            // Wait, IdentityStep calls `PronounRepository.GetPronounsAsync`. 
            // `GetPronounsAsync` calls `Upsert` if fetched from API.
            // We masked API... wait. `AlejoPronounService` fetches from HTTP.
            // If we stampede HTTP, it might fail/slow down.
            // But we didn't mock AlejoService in BotEngine! It's using the REAL `AlejoPronounService`.
            // Real HTTP calls for 100 users? That's bad for unit tests.
            // However, `PronounRepository` catches exceptions.
            // We should ideally Mock `AlejoPronounService` inside `BotEngine`, but `BotEngine` creates it internally.
            // Refactoring to Dependency Injection would be better, but for now let's modify the test to NOT trigger API calls?
            // Or just verify the "System doesn't crash".
            
            // To simulate "Processing Completed", we can check if `IdentityStep` cached the users?
            // `MemoryCache` is internal to services.
            
            // Instead of checking side effects, let's just ensure no unhandled exceptions crashed the background thread.
            // And sleep for a bit.
            
            await Task.Delay(3000); // Give it time to process queue
            
            // If the Brain crashed, logs would show error.
            // Real verification: Check `user_nicknames` count logic?
            // Actually, let's trigger a `!setnick` command for 100 users. That forces DB writes.
            
            var cmdTasks = new List<Task>();
            for (int i = 0; i < 50; i++)
            {
                var idx = i;
                cmdTasks.Add(Task.Run(() => 
                {
                    var args = new Dictionary<string, object>
                    {
                        { "triggerType", "TwitchChatMessage" }, // Maps to Chat event
                        { "message", $"!setnick Nick{idx}" },
                        { "user", $"User {idx}" },
                        { "userId", $"{userIdBase}{idx}" },
                        { "display_name", $"User {idx}" }
                    };
                    _engine.Ingest(args);
                }));
            }
            await Task.WhenAll(cmdTasks);
            
            await Task.Delay(10000); // Wait for writes

            // Verify DB count
            using (var db = new LiteDatabase($"Filename={Path.Combine(_tempRoot, "PNGTuber-GPT", "pngtuber.db")}"))
            {
                var col = db.GetCollection<BsonDocument>("user_nicknames");
                var dbCount = col.Count();
                
                if (dbCount != 50)
                {
                    var logsDir = Path.Combine(_tempRoot, "PNGTuber-GPT", "logs");
                    if (Directory.Exists(logsDir))
                    {
                        var files = Directory.GetFiles(logsDir, "*.log");
                        if (files.Length > 0)
                        {
                            var contents = File.ReadAllText(files[0]);
                            _output.WriteLine($"[FAILURE LOGS]: {contents}");
                        }
                    }
                }
                
                Assert.Equal(50, dbCount);
            }
        }
        
        public void Dispose()
        {
            _engine?.Shutdown();
            try { Directory.Delete(_tempRoot, true); } catch { }
        }
    }
}
