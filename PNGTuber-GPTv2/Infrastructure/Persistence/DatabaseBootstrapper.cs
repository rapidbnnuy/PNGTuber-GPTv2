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
            using (var dbMutex = new DatabaseMutex(_logger))
            {
                if (!dbMutex.Acquire(TimeSpan.FromSeconds(5)))
                {
                    _logger.Error("Could not acquire Database Mutex. Initialization aborted.");
                    return;
                }
                try
                {
                    _logger.Debug($"Connecting to Database at: {_dbPath} (Exclusive)");
                    EnsureDatabaseSeeded();
                }
                catch (Exception ex)
                {
                    _logger.Error($"Database Initialization Failed: {ex.Message}");
                }
            }
        }

        private void EnsureDatabaseSeeded()
        {
            using (var db = new LiteDatabase($"Filename={_dbPath}"))
            {
                EnsureSettings(db);
                EnsureIndices(db);
                db.Checkpoint();
            }
        }

        private void EnsureSettings(LiteDatabase db)
        {
            var settingsCol = db.GetCollection<AppSettings>("settings");
            var globalSettings = settingsCol.FindById("Global");
            if (globalSettings == null)
            {
                _logger.Info("Global Settings not found. Creating defaults.");
                settingsCol.Insert(AppSettings.CreateDefault());
            }
            else _logger.Debug("Global Settings loaded.");
        }

        private void EnsureIndices(LiteDatabase db)
        {
            db.GetCollection<User>("users").EnsureIndex(x => x.Id, true);
            db.GetCollection<LiteDB.BsonDocument>("pronouns").EnsureIndex("Name", true);
            db.GetCollection<LiteDB.BsonDocument>("user_pronouns").EnsureIndex("UserId"); 
            db.GetCollection<LiteDB.BsonDocument>("events").EnsureIndex("Type");
            db.GetCollection<LiteDB.BsonDocument>("interactions").EnsureIndex("UserId");
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
