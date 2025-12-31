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
            try
            {
                _logger.Debug($"Connecting to Database at: {_dbPath}");

                using (var db = new LiteDatabase($"Filename={_dbPath};Connection=Shared"))
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
                    var pronouns = db.GetCollection<BsonDocument>("pronouns");
                    pronouns.EnsureIndex("Name", true);

                    // User Pronouns: Index by UserId for fast lookups
                    var userPronouns = db.GetCollection<BsonDocument>("user_pronouns");
                    userPronouns.EnsureIndex("UserId"); 

                    // Events: Time-sortable ID is default, but maybe Type?
                    var events = db.GetCollection<BsonDocument>("events");
                    events.EnsureIndex("Type");

                    // Interactions: Index by UserId
                    var interactions = db.GetCollection<BsonDocument>("interactions");
                    interactions.EnsureIndex("UserId");

                    // Keywords: Index by Keyword (Id)
                    var keywords = db.GetCollection<BsonDocument>("keywords");
                    // _id is the keyword, so implicit index.

                    db.Checkpoint();
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Database Initialization Failed: {ex.Message}");
                // We do not rethrow here to avoid crashing CPH, but the plugin is likely broken.
            }
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
