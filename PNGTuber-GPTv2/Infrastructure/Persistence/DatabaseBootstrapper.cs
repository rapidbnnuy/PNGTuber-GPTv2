using System;
using System.IO;
using LiteDB;
using PNGTuber_GPTv2.Core.Interfaces;
using PNGTuber_GPTv2.Domain.Entities;

namespace PNGTuber_GPTv2.Infrastructure.Persistence
{
    public class DatabaseBootstrapper
    {
        private readonly string _dbPath;
        private readonly ILogger _logger;

        public DatabaseBootstrapper(string pluginDir, ILogger logger)
        {
            _dbPath = Path.Combine(pluginDir, "PNGTuber-GPT.db");
            _logger = logger;
        }

        public void Initialize()
        {
            // Use our Mutex wrapper
            using (var dbMutex = new DatabaseMutex(_logger))
            {
                // Wait up to 5 seconds for the lock
                if (!dbMutex.Acquire(TimeSpan.FromSeconds(5)))
                {
                    _logger.Error("Could not acquire Database Mutex. Initialization aborted.");
                    return;
                }

                try
                {
                    _logger.Debug($"Connecting to Database at: {_dbPath} (Exclusive)");

                    // Removed Connection=Shared. Default is Direct/Exclusive.
                    using (var db = new LiteDatabase($"Filename={_dbPath}"))
                    {
                        // 1. Settings
                        var settingsCol = db.GetCollection<AppSettings>("settings");
                        var globalSettings = settingsCol.FindById("Global");

                        if (globalSettings == null)
                        {
                            _logger.Info("Global Settings not found. Creating defaults.");
                            globalSettings = AppSettings.CreateDefault();
                            settingsCol.Insert(globalSettings);
                        }
                        else
                        {
                            _logger.Debug("Global Settings loaded.");
                        }

                        // 2. Ensure Indices for Core Schema
                        // Users: Index by Id (Platform:Id)
                        var users = db.GetCollection<User>("users");
                        users.EnsureIndex(x => x.Id, true); // Unique

                        // Pronouns: Index by Name (e.g. He/Him)
                        var pronouns = db.GetCollection<LiteDB.BsonDocument>("pronouns");
                        pronouns.EnsureIndex("Name", true);

                        // User Pronouns: Index by UserId for fast lookups
                        var userPronouns = db.GetCollection<LiteDB.BsonDocument>("user_pronouns");
                        userPronouns.EnsureIndex("UserId"); 

                        // Events: Time-sortable ID is default, but maybe Type?
                        var events = db.GetCollection<LiteDB.BsonDocument>("events");
                        events.EnsureIndex("Type");

                        // Interactions: Index by UserId
                        var interactions = db.GetCollection<LiteDB.BsonDocument>("interactions");
                        interactions.EnsureIndex("UserId");

                        // Keywords: Index by Keyword (Id)
                        var keywords = db.GetCollection<LiteDB.BsonDocument>("keywords");
                        // _id is the keyword, so implicit index.

                        db.Checkpoint();
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"Database Initialization Failed: {ex.Message}");
                    // We do not rethrow here to avoid crashing CPH, but the plugin is likely broken.
                }
            } // Mutex released on Dispose
        }
        public void PruneLockFile()
        {
            try
            {
                var lockFilePath = $"{_dbPath}-lock";
                
                if (File.Exists(lockFilePath))
                {
                    _logger.Warn($"Found Database Lock File: {lockFilePath}. Attempting removal...");
                    File.Delete(lockFilePath);
                    _logger.Info("Database Lock File removed successfully.");
                }
                else
                {
                    _logger.Debug("No Database Lock File found.");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to prune Database Lock File: {ex.Message}");
            }
        }
    }
}
