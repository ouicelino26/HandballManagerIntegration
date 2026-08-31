using HandballIntegration.Data;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using HandballIntegration.Admin.Abstractions;
using HandballIntegration.Admin.Models;

namespace HandballIntegration.Services
{
    public class ApiAuthService : IApiAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly ApiSettings _settings;
        private readonly IAdminSessionService _sessionService;

        public string? AccessToken { get; private set; }
        public string? Username { get; private set; }
        public string? Role { get; private set; }
        public bool IsAdmin => string.Equals(Role, "Admin", StringComparison.OrdinalIgnoreCase)
            || string.Equals(Role, "ADMIN", StringComparison.Ordinal)
            || string.Equals(Role, "SUPER_ADMIN", StringComparison.Ordinal);

        public ApiAuthService(
            HttpClient httpClient,
            IOptions<ApiSettings> options,
            IAdminSessionService sessionService)
        {
            _httpClient = httpClient;
            _settings = options.Value;
            _sessionService = sessionService;
        }

        public Task<bool> AuthenticateAsync()
        {
            return Task.FromResult(!string.IsNullOrWhiteSpace(AccessToken) && _sessionService.IsAuthenticated);
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
            catch (Exception)
            {
                return new ApiLoginResult
                {
                    Success = false,
                    Message = "Connexion impossible a l'API. Verifiez le reseau puis reessayez."
                };
            }

            if (!response.IsSuccessStatusCode)
            {
                return new ApiLoginResult
                {
                    Success = false,
                    Message = response.StatusCode == System.Net.HttpStatusCode.Unauthorized
                        ? "Identifiants invalides."
                        : "La connexion a ete refusee par le serveur."
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

            AccessToken = result.accesstoken;
            Username = result.username;
            Role = NormalizeAdminRole(result.adminRole ?? result.role);
            var permissions = (result.permissions ?? [])
                .ToHashSet(StringComparer.Ordinal);
            _sessionService.Set(new AdminSession(
                AccessToken,
                Username ?? string.Empty,
                Role,
                permissions,
                ReadExpirationUtc(AccessToken) ?? DateTime.UtcNow.AddMinutes(30)));

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
            _sessionService.Clear();
        }

        public bool IsTokenExpiringSoon()
        {
            var expiresAtUtc = _sessionService.Current?.ExpiresAtUtc;
            if (expiresAtUtc == null)
                return true;
            return expiresAtUtc.Value - DateTime.UtcNow < TimeSpan.FromMinutes(2);
        }

        private static string NormalizeAdminRole(string? role)
        {
            if (string.Equals(role, "Admin", StringComparison.Ordinal)) return "SUPER_ADMIN";
            if (string.Equals(role, "Consultation", StringComparison.Ordinal)) return "VIEWER";
            return string.IsNullOrWhiteSpace(role) ? "VIEWER" : role;
        }

        private static DateTime? ReadExpirationUtc(string token)
        {
            try
            {
                var segments = token.Split('.');
                if (segments.Length < 2) return null;
                var payload = segments[1].Replace('-', '+').Replace('_', '/');
                payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
                using var document = JsonDocument.Parse(Convert.FromBase64String(payload));
                return document.RootElement.TryGetProperty("exp", out var expiration)
                    ? DateTimeOffset.FromUnixTimeSeconds(expiration.GetInt64()).UtcDateTime
                    : null;
            }
            catch
            {
                return null;
            }
        }

        private class LoginResponse
        {
            public string? accesstoken { get; set; }
            public string? username { get; set; }
            public string? role { get; set; }
            public string? adminRole { get; set; }
            public List<string>? permissions { get; set; }
        }
    }
}
