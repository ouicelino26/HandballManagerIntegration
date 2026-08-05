using System.Net.Http.Json;
using System.Net.Http;
using HandballIntegration.Admin.Abstractions;
using HandballIntegration.Admin.Models;

namespace HandballIntegration.Admin.Services;

public sealed class AdminApiClient(
    HttpClient httpClient,
    IProblemDetailsMapper problemDetailsMapper) : IAdminApiClient
{
    public async Task<AdminCapabilitiesResponse> GetCapabilitiesAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(
            "api/v2/admin/capabilities",
            cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new AdminApiException(problemDetailsMapper.Map(response.StatusCode, body));
        }

        return await response.Content.ReadFromJsonAsync<AdminCapabilitiesResponse>(
                   cancellationToken: cancellationToken)
               ?? throw new AdminApiException(problemDetailsMapper.Map(
                   System.Net.HttpStatusCode.InternalServerError,
                   null));
    }
}
