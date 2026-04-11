using HandballIntegration.Data;
using HandballManagerCore.DTO;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace HandballIntegration.Services
{
    public class PlayersApiService
    {
        private readonly HttpClient _http;
        private readonly IApiAuthService _auth;
        private readonly ApiSettings _settings;

        public PlayersApiService(HttpClient http, IApiAuthService auth, Microsoft.Extensions.Options.IOptions<ApiSettings> options)
        {
            _http = http;
            _auth = auth;
            _settings = options.Value;
        }

        public async Task<List<PlayerDto>?> GetPlayersAsync()
        {
            try
            {
                bool ok = await _auth.AuthenticateAsync();
                if (!ok)
                {
                    return null;
                }

                _auth.ApplyAuthorizationHeader(_http);

                var response = await _http.GetAsync($"{_settings.BaseUrl}api/Players");

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();

                return JsonSerializer.Deserialize<List<PlayerDto>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> DeletePlayerAsync(int playerId)
        {
            try
            {
                bool ok = await _auth.AuthenticateAsync();
                if (!ok)
                {
                    return false;
                }

                _auth.ApplyAuthorizationHeader(_http);

                var response = await _http.DeleteAsync($"{_settings.BaseUrl}api/Players/{playerId}");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
