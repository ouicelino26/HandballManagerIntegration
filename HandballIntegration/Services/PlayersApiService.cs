using HandballIntegration.Data;
using HandballManagerCore.DTO;
using HandballManagerCore.Models;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace HandballIntegration.Services
{
    public class PlayersApiService
    {
        private readonly HttpClient _http;
        private readonly IApiAuthService _auth;
        private readonly ApiSettings _settings;

        public PlayersApiService(HttpClient http, IApiAuthService auth, IOptions<ApiSettings> options)
        {
            _http = http;
            _auth = auth;
            _settings = options.Value;
        }

        public async Task<List<PlayerListItemDto>?> GetPlayersAsync()
        {
            try
            {
                bool ok = await _auth.AuthenticateAsync();
                if (!ok)
                {
                    return null;
                }

                _auth.ApplyAuthorizationHeader(_http);

                const int pageSize = 500;
                int page = 1;
                var players = new List<PlayerListItemDto>();

                while (true)
                {
                    var response = await _http.GetAsync($"{_settings.ApiBaseUrl}api/Players?page={page}&pageSize={pageSize}");

                    if (!response.IsSuccessStatusCode)
                    {
                        return null;
                    }

                    var json = await response.Content.ReadAsStringAsync();
                    var pagePlayers = JsonSerializer.Deserialize<List<PlayerListItemDto>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (pagePlayers == null || pagePlayers.Count == 0)
                    {
                        break;
                    }

                    players.AddRange(pagePlayers);

                    if (pagePlayers.Count < pageSize)
                    {
                        break;
                    }

                    page++;
                }

                return players;
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> UpdatePlayerStatusAsync(int playerId, bool isActive)
        {
            return await UpdatePlayerAsync(playerId, new UpdatePlayerRequest
            {
                IsActive = isActive
            });
        }

        public async Task<bool> UpdatePlayerTeamAsync(int playerId, int teamId)
        {
            return await UpdatePlayerAsync(playerId, new UpdatePlayerRequest
            {
                TeamId = teamId
            });
        }

        public async Task<bool> UpdatePlayerAsync(PlayerEditionRequest request)
        {
            return await UpdatePlayerAsync(request.PlayerId, new UpdatePlayerRequest
            {
                Name = request.FirstName,
                Surname = request.LastName,
                Birthday = request.Birthday,
                Age = request.Age,
                PositionId = request.PositionId,
                TeamId = request.TeamId,
                NationalityId = request.NationalityId,
                Number = request.Number,
                IsActive = request.IsActive
            });
        }

        private async Task<bool> UpdatePlayerAsync(int playerId, UpdatePlayerRequest request)
        {
            try
            {
                if (!await _auth.AuthenticateAsync())
                {
                    return false;
                }

                _auth.ApplyAuthorizationHeader(_http);

                var response = await _http.PutAsJsonAsync(
                    $"{_settings.ApiBaseUrl}api/Players/{playerId}",
                    request);

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<TeamDto>> GetTeamsAsync()
        {
            try
            {
                if (!await _auth.AuthenticateAsync())
                {
                    return new List<TeamDto>();
                }

                _auth.ApplyAuthorizationHeader(_http);

                return await _http.GetFromJsonAsync<List<TeamDto>>($"{_settings.ApiBaseUrl}api/Teams")
                    ?? new List<TeamDto>();
            }
            catch
            {
                return new List<TeamDto>();
            }
        }

        public async Task<List<LookupItemDto>> GetPositionsAsync()
        {
            try
            {
                if (!await _auth.AuthenticateAsync())
                {
                    return new List<LookupItemDto>();
                }

                _auth.ApplyAuthorizationHeader(_http);

                var positions = await _http.GetFromJsonAsync<List<LookupItemDto>>(
                    $"{_settings.ApiBaseUrl}api/Lookups/positions");

                return positions ?? new List<LookupItemDto>();
            }
            catch
            {
                return new List<LookupItemDto>();
            }
        }

        public async Task<List<LookupItemDto>> GetNationalitiesAsync()
        {
            try
            {
                if (!await _auth.AuthenticateAsync())
                {
                    return new List<LookupItemDto>();
                }

                _auth.ApplyAuthorizationHeader(_http);

                var lookupNationalities = await _http.GetFromJsonAsync<List<LookupItemDto>>(
                    $"{_settings.ApiBaseUrl}api/Lookups/nationalities");

                if (lookupNationalities?.Any() == true)
                {
                    return lookupNationalities;
                }

                var legacyNationalities = await _http.GetFromJsonAsync<List<Nationality>>(
                    $"{_settings.ApiBaseUrl}api/Nationalities");

                return legacyNationalities?
                    .Select(item => new LookupItemDto
                    {
                        Id = item.Id,
                        Name = string.IsNullOrWhiteSpace(item.NationalityF) ? item.Country : item.NationalityF
                    })
                    .ToList()
                    ?? new List<LookupItemDto>();
            }
            catch
            {
                return new List<LookupItemDto>();
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

                var response = await _http.DeleteAsync($"{_settings.ApiBaseUrl}api/Players/{playerId}");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private sealed class UpdatePlayerRequest
        {
            public string? Name { get; set; }
            public string? Surname { get; set; }
            public System.DateTime? Birthday { get; set; }
            public int? Age { get; set; }
            public int? PositionId { get; set; }
            public int? TeamId { get; set; }
            public int? NationalityId { get; set; }
            public int? Number { get; set; }
            public bool? IsActive { get; set; }
        }

        public sealed class PlayerEditionRequest
        {
            public int PlayerId { get; set; }
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public System.DateTime? Birthday { get; set; }
            public int? Age { get; set; }
            public int? PositionId { get; set; }
            public int? TeamId { get; set; }
            public int? NationalityId { get; set; }
            public int? Number { get; set; }
            public bool IsActive { get; set; }
        }
    }
}
