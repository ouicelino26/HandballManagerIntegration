using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using HandballIntegration.Admin.Abstractions;
using HandballIntegration.Admin.Models;
using HandballIntegration.Core.Abstractions;
using HandballIntegration.Core.Models;

namespace HandballIntegration.Infrastructure.Api;

public sealed class AdminApiTransport(
    HttpClient httpClient,
    IProblemDetailsMapper problemDetailsMapper,
    ICorrelationIdProvider correlationIdProvider) : IAdminApiTransport
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly string ClientVersion =
        Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "unknown";

    public Task<AdminHttpResult<T>> GetAsync<T>(
        string relativeUri,
        CancellationToken cancellationToken = default) =>
        SendAsync<T>(HttpMethod.Get, relativeUri, null, null, cancellationToken);

    public Task<AdminHttpResult<T>> SendJsonAsync<T>(
        HttpMethod method,
        string relativeUri,
        object body,
        string? ifMatch = null,
        CancellationToken cancellationToken = default) =>
        SendAsync<T>(method, relativeUri, JsonContent.Create(body, options: JsonOptions), ifMatch, cancellationToken);

    public Task<AdminHttpResult<T>> SendContentAsync<T>(
        HttpMethod method,
        string relativeUri,
        HttpContent content,
        string? ifMatch = null,
        CancellationToken cancellationToken = default) =>
        SendAsync<T>(method, relativeUri, content, ifMatch, cancellationToken);

    private async Task<AdminHttpResult<T>> SendAsync<T>(
        HttpMethod method,
        string relativeUri,
        HttpContent? content,
        string? ifMatch,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, relativeUri) { Content = content };
        var correlationId = correlationIdProvider.Create();
        request.Headers.TryAddWithoutValidation("X-Correlation-Id", correlationId);
        request.Headers.TryAddWithoutValidation("X-Client-Version", ClientVersion);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (!string.IsNullOrWhiteSpace(ifMatch))
        {
            request.Headers.TryAddWithoutValidation("If-Match", ifMatch);
        }

        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            throw Offline(HttpStatusCode.RequestTimeout, correlationId);
        }
        catch (HttpRequestException)
        {
            throw Offline(HttpStatusCode.ServiceUnavailable, correlationId);
        }

        using (response)
        {
            var responseCorrelationId = ReadHeader(response, "X-Correlation-Id") ?? correlationId;
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                var mapped = problemDetailsMapper.Map(response.StatusCode, body);
                var error = mapped with
                {
                    CorrelationId = mapped.CorrelationId ?? responseCorrelationId
                };
                throw new AdminApiException(error);
            }

            var value = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken)
                        ?? throw new AdminApiException(problemDetailsMapper.Map(
                            HttpStatusCode.InternalServerError,
                            null));
            return new AdminHttpResult<T>(
                value,
                response.Headers.ETag?.ToString(),
                ReadHeader(response, "X-Audit-Id"),
                responseCorrelationId);
        }
    }

    private AdminApiException Offline(HttpStatusCode status, string correlationId)
    {
        var mapped = problemDetailsMapper.Map(status, null) with { CorrelationId = correlationId };
        return new AdminApiException(mapped);
    }

    private static string? ReadHeader(HttpResponseMessage response, string name) =>
        response.Headers.TryGetValues(name, out var values) ? values.FirstOrDefault() : null;
}
