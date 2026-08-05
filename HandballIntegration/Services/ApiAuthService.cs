using HandballIntegration.Data;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace HandballIntegration.Services
{
    public class ApiAuthService : IApiAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly ApiSettings _settings;

        public string? AccessToken { get; private set; }
        public string? Username { get; private set; }
        public string? Role { get; private set; }
        public bool IsAdmin => string.Equals(Role, "Admin", StringComparison.OrdinalIgnoreCase);

        public ApiAuthService(HttpClient httpClient, IOptions<ApiSettings> options)
        {
            _httpClient = httpClient;
            _settings = options.Value;
        }

        public Task<bool> AuthenticateAsync()
        {
            return Task.FromResult(!string.IsNullOrWhiteSpace(AccessToken) && IsAdmin);
        }

        public async Task<ApiLoginResult> LoginAsync(string username, string password)
        {
            Logout();

            var request = new
            {
                Username = username?.Trim(),
                Password = password
            };

            HttpResponseMessage response;

            try
            {
                response = await _httpClient.PostAsJsonAsync($"{_settings.ApiBaseUrl}auth/login", request);
            }
            catch (Exception ex)
            {
                return new ApiLoginResult
                {
                    Success = false,
                    Message = $"Connexion impossible a l'API : {ex.Message}"
                };
            }

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return new ApiLoginResult
                {
                    Success = false,
                    Message = string.IsNullOrWhiteSpace(error) ? "Identifiants invalides." : error
                };
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<LoginResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (string.IsNullOrWhiteSpace(result?.accesstoken))
            {
                return new ApiLoginResult
                {
                    Success = false,
                    Message = "La reponse de connexion ne contient pas de token."
                };
            }

            if (!string.Equals(result.role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                return new ApiLoginResult
                {
                    Success = false,
                    Message = "Acces reserve aux administrateurs.",
                    Username = result.username,
                    Role = result.role
                };
            }

            AccessToken = result.accesstoken;
            Username = result.username;
            Role = result.role;

            return new ApiLoginResult
            {
                Success = true,
                Message = "Connexion etablie.",
                Username = Username,
                Role = Role
            };
        }

        public void ApplyAuthorizationHeader(HttpClient client)
        {
            if (string.IsNullOrWhiteSpace(AccessToken))
            {
                client.DefaultRequestHeaders.Authorization = null;
                return;
            }

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", AccessToken);
        }

        public void Logout()
        {
            AccessToken = null;
            Username = null;
            Role = null;
        }

        private class LoginResponse
        {
            public string? accesstoken { get; set; }
            public string? username { get; set; }
            public string? role { get; set; }
        }
    }
}
