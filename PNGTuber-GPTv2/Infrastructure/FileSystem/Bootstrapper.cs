using System;
using System.IO;
using PNGTuber_GPTv2.Domain.Constants;

namespace PNGTuber_GPTv2.Infrastructure.FileSystem
{
    public class Bootstrapper
    {
        // Returns the base path of the plugin: %InstallDir%/PNGTuber-GPT/
        public static string Initialize(string installDir)
        {
            if (string.IsNullOrWhiteSpace(installDir))
                installDir = AppDomain.CurrentDomain.BaseDirectory;

            string pluginDir = Path.Combine(installDir, "PNGTuber-GPT");
            string logsDir = Path.Combine(pluginDir, "Logs");

            // 1. Create Core Directories
            if (!Directory.Exists(pluginDir)) Directory.CreateDirectory(pluginDir);
            if (!Directory.Exists(logsDir)) Directory.CreateDirectory(logsDir);

            // 2. Ensure Context.txt exists
            string contextPath = Path.Combine(pluginDir, "Context.txt");
            if (!File.Exists(contextPath))
            {
                File.WriteAllText(contextPath, SystemPrompts.DefaultContext);
            }

            return pluginDir;
        }
    }
}
