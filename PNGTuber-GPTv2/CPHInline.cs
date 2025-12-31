using System;
using System.Collections.Generic;
using Streamer.bot.Plugin.Interface;

namespace PNGTuber_GPTv2
{
    public class CPHInline
    {
        public IInlineInvokeProxy CPH { get; set; }

        public void Execute()
        {
            CPH.LogInfo("PNGTuber-GPTv2 Plugin Initialized!");
        }
    }
}
