using System;
using System.Collections.Generic;
using System.Threading.Channels;
using LiteDB;
using Streamer.bot.Plugin.Interface;

namespace PNGTuber_GPTv2
{
    public class CPHInline
    {
        public IInlineInvokeProxy CPH { get; set; }

        public void Execute()
        {
            CPH.LogInfo("PNGTuber-GPTv2 Plugin Initialized!");

            // Verify System.Threading.Channels
            var channel = Channel.CreateUnbounded<string>();
            CPH.LogInfo($"Channel created with capacity: {channel.Reader.CanCount}");

            // Verify LiteDB
            try 
            {
                using(var db = new LiteDatabase(":memory:"))
                {
                    var col = db.GetCollection<BsonDocument>("test");
                    col.Insert(new BsonDocument { ["msg"] = "LiteDB Works!" });
                    CPH.LogInfo($"LiteDB Insert count: {col.Count()}");
                }
            }
            catch(Exception ex)
            {
                CPH.LogInfo($"LiteDB Error: {ex.Message}");
            }
        }
    }
}
