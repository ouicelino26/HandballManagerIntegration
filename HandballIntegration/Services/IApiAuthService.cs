using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandballIntegration.Services
{
    public interface IApiAuthService
    {
        Task<bool> AuthenticateAsync();
        Task<ApiLoginResult> LoginAsync(string username, string password);
        void ApplyAuthorizationHeader(System.Net.Http.HttpClient client);
        void Logout();
        string? AccessToken { get; }
        string? Username { get; }
        string? Role { get; }
        bool IsAdmin { get; }
    }

    public class ApiLoginResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Username { get; set; }
        public string? Role { get; set; }
    }
}
