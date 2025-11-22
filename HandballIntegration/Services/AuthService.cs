using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System;
namespace HandballIntegration.Services
{
    public class AuthService
    {
        private readonly HttpClient _http;

        public AuthService()
        {
            _http = new HttpClient
            {
                BaseAddress = new Uri("http://89.168.39.146:5000")
            };
        }

        public async Task<string?> GetTokenAsync()
        {
            var request = new
            {
                ClientId = "my-HandApp-id",
                ClientSecret = "sg321sef6e5sfes321fse3f21"
            };

            var response = await _http.PostAsJsonAsync("auth/token", request);

            if (!response.IsSuccessStatusCode)
                return null;

            var result = await response.Content.ReadFromJsonAsync<TokenResponse>();

            return result?.accesstoken;
        }
    }

    public class TokenResponse
    {
        public string accesstoken { get; set; }
    }
}
