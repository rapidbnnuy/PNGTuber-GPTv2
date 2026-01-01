using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using PNGTuber_GPTv2.Core.Interfaces;
using PNGTuber_GPTv2.Domain.Structs;

namespace PNGTuber_GPTv2.Infrastructure.External
{
    public class AlejoPronounService : IPronounService, IDisposable
    {
        private readonly HttpClient _http;
        private readonly ILogger _logger;
        private const string ApiUrl = "https://pronouns.alejo.io/api/users";

        public AlejoPronounService(ILogger logger)
        {
            _logger = logger;
            _http = new HttpClient();
            _http.Timeout = TimeSpan.FromSeconds(2);
        }

        public async Task<Pronouns?> FetchPronounsAsync(string platformId, CancellationToken ct)
        {
            try
            {
                var response = await _http.GetAsync($"{ApiUrl}/{platformId}", ct);
                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode != System.Net.HttpStatusCode.NotFound)
                        _logger.Warn($"[Alejo] API Error {response.StatusCode} for {platformId}");
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var foundId = ExtractPronounId(json);

                if (!string.IsNullOrEmpty(foundId))
                {
                    var p = Pronouns.MapFromId(foundId);
                    _logger.Debug($"[Alejo] Mapped '{foundId}' -> {p.Display}");
                    return p;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.Warn($"[Alejo] Fetch Failed: {ex.Message}");
                return null;
            }
        }

        private string ExtractPronounId(string json)
        {
            if (json.Contains("\"name\":\""))
                return ExtractValue(json, "\"name\":\"", "\"");
            if (json.Contains("\"pronoun_id\":\""))
                return ExtractValue(json, "\"pronoun_id\":\"", "\"");
            return null;
        }

        private string ExtractValue(string source, string startTag, string endTag)
        {
            var startIndex = source.IndexOf(startTag);
            if (startIndex == -1) return null;
            
            startIndex += startTag.Length;
            var endIndex = source.IndexOf(endTag, startIndex);
            if (endIndex == -1) return null;
            
            return source.Substring(startIndex, endIndex - startIndex);
        }
        public void Dispose()
        {
            _http?.Dispose();
        }
    }
}
