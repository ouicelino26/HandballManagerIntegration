using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using HandballIntegration.Data;
using System.Net.Http.Json;
    

namespace HandballIntegration.Services
{
    public class ApiAuthService : IApiAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly ApiSettings _settings;

        public string AccessToken { get; private set; }

        public ApiAuthService(HttpClient httpClient, IOptions<ApiSettings> options)
        {
            _httpClient = httpClient;
            _settings = options.Value;
        }

        public async Task<bool> AuthenticateAsync()
        {
            if (!string.IsNullOrEmpty(AccessToken))
                return true;

            var credentials = new
            {
                clientId = _settings.ClientId,
                clientSecret = _settings.ClientSecret
            };

            var response = await _httpClient.PostAsJsonAsync($"{_settings.BaseUrl}auth/token", credentials);

            if (!response.IsSuccessStatusCode)
                return false;

            var json = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<TokenResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            AccessToken = result?.accesstoken;
            return !string.IsNullOrEmpty(AccessToken);
        }
    }
}
