using HandballIntegration.Data;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace HandballIntegration.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IApiAuthService _authService;
        private readonly string _baseUrl;

        public string? CurrentUsername => _authService.Username;
        public string? CurrentRole => _authService.Role;

        public ApiService(HttpClient httpClient, IApiAuthService authService, IOptions<ApiSettings> options)
        {
            _httpClient = httpClient;
            _authService = authService;
            _baseUrl = options.Value.ApiBaseUrl;
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                if (!await _authService.AuthenticateAsync())
                {
                    return false;
                }

                _authService.ApplyAuthorizationHeader(_httpClient);

                var response = await _httpClient.GetAsync($"{_baseUrl}api/Users/me");
                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }

                var json = await response.Content.ReadAsStringAsync();
                var profile = JsonSerializer.Deserialize<UserProfileResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return profile is not null &&
                       string.Equals(profile.Role, "Admin", System.StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> PrepareAuthorizedClientAsync(HttpClient client)
        {
            if (!await _authService.AuthenticateAsync())
            {
                return false;
            }

            _authService.ApplyAuthorizationHeader(client);
            return true;
        }

        private class UserProfileResponse
        {
            public string? Username { get; set; }
            public string? Role { get; set; }
        }
    }
}
