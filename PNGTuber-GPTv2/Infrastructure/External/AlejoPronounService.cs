using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using PNGTuber_GPTv2.Core.Interfaces;
using PNGTuber_GPTv2.Domain.Structs;

namespace PNGTuber_GPTv2.Infrastructure.External
{
    public class AlejoPronounService : IPronounService
    {
        private readonly HttpClient _http;
        private readonly ILogger _logger;
        private const string ApiUrl = "https://pronouns.alejo.io/api/users";

        public AlejoPronounService(ILogger logger)
        {
            _logger = logger;
            _http = new HttpClient();
            _http.Timeout = TimeSpan.FromSeconds(2); // Fast fail
        }

        public async Task<Pronouns?> FetchPronounsAsync(string platformId, CancellationToken ct)
        {
            try
            {
                // Alejo uses Twitch ID or Login. We prefer ID.
                var response = await _http.GetAsync($"{ApiUrl}/{platformId}", ct);
                
                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode != System.Net.HttpStatusCode.NotFound)
                        _logger.Warn($"[Alejo] API Error {response.StatusCode} for {platformId}");
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                
                // Very naive JSON parsing to avoid heavy Newtonsoft dependency if possible, 
                // but for reliability we should parse properly. 
                // Alejo returns: [ { "pronoun_id": "hehim", ... } ] or []
                
                // For this implementation, we map common IDs manually to our struct.
                // "hehim" -> He/Him
                // "sheher" -> She/Her
                // "theythem" -> They/Them
                
                if (json.Contains("\"pronoun_id\":\"hehim\"")) return new Pronouns("He/Him", "He", "Him", "His", "His");
                if (json.Contains("\"pronoun_id\":\"sheher\"")) return new Pronouns("She/Her", "She", "Her", "Her", "Hers");
                if (json.Contains("\"pronoun_id\":\"theythem\"")) return new Pronouns("They/Them", "They", "Them", "Their", "Theirs");
                if (json.Contains("\"pronoun_id\":\"other\"")) return Pronouns.TheyThem; // Fallback safe
                
                return null; // None found
            }
            catch (Exception ex)
            {
                _logger.Warn($"[Alejo] Fetch Failed: {ex.Message}");
                return null;
            }
        }
    }
}
