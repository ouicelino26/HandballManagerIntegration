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
        string? AccessToken { get; }
    }
}
