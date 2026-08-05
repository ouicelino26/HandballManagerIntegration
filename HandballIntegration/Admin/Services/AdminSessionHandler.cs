using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using HandballIntegration.Admin.Abstractions;
using HandballIntegration.Admin.Models;

namespace HandballIntegration.Admin.Services;

public sealed class AdminSessionHandler(IAdminSessionService sessionService) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (sessionService.IsExpired)
        {
            sessionService.Clear();
            throw new AdminSessionExpiredException();
        }

        var session = sessionService.Current;
        if (session is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        }

        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            sessionService.Clear();
        }

        return response;
    }
}
