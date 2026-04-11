using HandballIntegration.Data;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace HandballIntegration.Services
{
    public class UsersApiService
    {
        private readonly HttpClient _http;
        private readonly ApiService _apiService;
        private readonly ApiSettings _settings;

        public UsersApiService(HttpClient http, ApiService apiService, IOptions<ApiSettings> options)
        {
            _http = http;
            _apiService = apiService;
            _settings = options.Value;
        }

        public async Task<UsersQueryResult> GetUsersAsync()
        {
            if (!await _apiService.PrepareAuthorizedClientAsync(_http))
            {
                return new UsersQueryResult
                {
                    Success = false,
                    Message = "Session administrateur requise."
                };
            }

            try
            {
                var response = await _http.GetAsync($"{_settings.BaseUrl}api/Users");
                if (!response.IsSuccessStatusCode)
                {
                    return new UsersQueryResult
                    {
                        Success = false,
                        Message = await ReadErrorMessageAsync(response)
                    };
                }

                var users = await response.Content.ReadFromJsonAsync<List<ManagedUserDto>>() ?? new List<ManagedUserDto>();

                return new UsersQueryResult
                {
                    Success = true,
                    Message = $"{users.Count} compte(s) charge(s).",
                    Users = users
                };
            }
            catch (Exception ex)
            {
                return new UsersQueryResult
                {
                    Success = false,
                    Message = $"Impossible de charger les comptes : {ex.Message}"
                };
            }
        }

        public async Task<UserOperationResult> CreateUserAsync(CreateManagedUserRequest request)
        {
            if (!await _apiService.PrepareAuthorizedClientAsync(_http))
            {
                return new UserOperationResult
                {
                    Success = false,
                    Message = "Session administrateur requise."
                };
            }

            try
            {
                var response = await _http.PostAsJsonAsync($"{_settings.BaseUrl}api/Users", request);
                if (!response.IsSuccessStatusCode)
                {
                    return new UserOperationResult
                    {
                        Success = false,
                        Message = await ReadErrorMessageAsync(response)
                    };
                }

                var createdUser = await response.Content.ReadFromJsonAsync<ManagedUserDto>();

                return new UserOperationResult
                {
                    Success = true,
                    Message = createdUser is null
                        ? "Compte cree."
                        : $"Compte cree pour {createdUser.Username}.",
                    User = createdUser
                };
            }
            catch (Exception ex)
            {
                return new UserOperationResult
                {
                    Success = false,
                    Message = $"Creation impossible : {ex.Message}"
                };
            }
        }

        private static async Task<string> ReadErrorMessageAsync(HttpResponseMessage response)
        {
            var body = await response.Content.ReadAsStringAsync();
            return string.IsNullOrWhiteSpace(body)
                ? $"Erreur API ({(int)response.StatusCode})."
                : body;
        }
    }

    public class ManagedUserDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }

        public string EmailDisplay => string.IsNullOrWhiteSpace(Email) ? "Non renseigne" : Email;
        public string StatusLabel => IsActive ? "Actif" : "Inactif";
    }

    public class CreateManagedUserRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = "Consultation";
    }

    public class UsersQueryResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<ManagedUserDto> Users { get; set; } = new List<ManagedUserDto>();
    }

    public class UserOperationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public ManagedUserDto User { get; set; }
    }
}
