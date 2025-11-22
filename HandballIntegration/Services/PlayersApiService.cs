using HandballIntegration.Data;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using HandballManagerCore.DTO;
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
            
            bool ok = await _auth.AuthenticateAsync();
            if (!ok) return null;

          
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _auth.AccessToken);

            
            var response = await _http.GetAsync($"{_settings.BaseUrl}api/Players");

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<List<PlayerDto>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
    }


}
