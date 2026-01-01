using System;
using System.Collections.Generic;

namespace PNGTuber_GPTv2.Domain.Entities
{
    public class AppSettings
    {
        public const string GlobalId = "Global";
        public string Id { get; set; } = GlobalId;

        public string OpenApiKey { get; set; }
        public string OpenAiModel { get; set; }
        public string ModelInputCost { get; set; }
        public string ModelOutputCost { get; set; }
        public string IgnoreBotUsernames { get; set; }

        public string CharacterVoiceAlias_1 { get; set; }
        public string CharacterVoiceAlias_2 { get; set; }
        public string CharacterVoiceAlias_3 { get; set; }
        public string CharacterVoiceAlias_4 { get; set; }
        public string CharacterVoiceAlias_5 { get; set; }

        public string CharacterFile_1 { get; set; }
        public string CharacterFile_2 { get; set; }
        public string CharacterFile_3 { get; set; }
        public string CharacterFile_4 { get; set; }
        public string CharacterFile_5 { get; set; }

        public string CompletionsEndpoint { get; set; }

        public string LoggingLevel { get; set; }
        public string StreamerId { get; set; }
        public string StreamerPlatform { get; set; } = "Twitch";
        
        public string HateThreshold { get; set; }
        public string HateThreateningThreshold { get; set; }
        public string HarassmentThreshold { get; set; }
        public string HarassmentThreateningThreshold { get; set; }
        public string SexualThreshold { get; set; }
        public string ViolenceThreshold { get; set; }
        public string ViolenceGraphicThreshold { get; set; }
        public string SelfHarmThreshold { get; set; }
        public string SelfHarmIntentThreshold { get; set; }
        public string SelfHarmInstructionsThreshold { get; set; }
        public string IllicitThreshold { get; set; }
        public string IllicitViolentThreshold { get; set; }
        
        public string Version { get; set; }
        public string LogGptQuestionsToDiscord { get; set; }
        public string DiscordWebhookUrl { get; set; }
        public string DiscordBotUsername { get; set; }
        public string DiscordAvatarUrl { get; set; }
        public string PostToChat { get; set; }
        public string LimitResponsesTo500Characters { get; set; }

        public string VoiceEnabled { get; set; }
        public string OutboundWebhookUrl { get; set; }
        public string OutboundWebhookMode { get; set; }

        public bool ModerationEnabled { get; set; } = true;
        public List<string> BannedKeywords { get; set; } = new List<string>();
        public bool ModerationRebukeEnabled { get; set; } = true;
        public int MaxChatHistory { get; set; } = 20;
        public int MaxPromptHistory { get; set; } = 10;

        public static AppSettings CreateDefault()
        {
            return new AppSettings
            {
                Id = "Global",
                LoggingLevel = "INFO",
                Version = "2.0.0",
                ModerationEnabled = true,
                ModerationRebukeEnabled = true,
                MaxChatHistory = 20,
                MaxPromptHistory = 10,
                OpenAiModel = "gpt-4o",
            };
        }
    }
}
