using Microsoft.Extensions.Options;
using System.Net.Http;
using HandballIntegration.Data;
using System.Threading.Tasks;
namespace HandballIntegration.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IApiAuthService _authService;
        private readonly string _baseUrl;

        public ApiService(HttpClient httpClient, IApiAuthService authService, IOptions<ApiSettings> options)
        {
            _httpClient = httpClient;
            _authService = authService;
            _baseUrl = options.Value.BaseUrl;
        }
        public async Task<bool> TestConnectionAsync()
        {
            // Tente de t'authentifier
            var ok = await _authService.AuthenticateAsync();
            return ok;
        }

    }
}
