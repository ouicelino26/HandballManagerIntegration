using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using HandballIntegration.Core.Abstractions;
using HandballIntegration.Core.Models;
using HandballManagerCore.DTO;
using HandballManagerCore.Models;

namespace HandballIntegration.Infrastructure.Api;

public sealed class AdminDashboardApiClient(IAdminApiTransport transport) : IAdminDashboardApiClient
{
    public async Task<AdminSystemVersion> GetVersionAsync(CancellationToken cancellationToken = default) =>
        (await transport.GetAsync<AdminSystemVersion>("api/system/version", cancellationToken)).Value;
}

public sealed class AdminImportApiClient(IAdminApiTransport transport) : IAdminImportApiClient
{
    public async Task<AdminImportPreview> PreviewAsync(
        string filePath,
        DateTime matchDate,
        string season,
        string day,
        int competitionId,
        CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(filePath);
        using var content = new MultipartFormDataContent();
        using var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        content.Add(fileContent, "file", Path.GetFileName(filePath));
        content.Add(new StringContent(matchDate.ToString("O", CultureInfo.InvariantCulture)), "matchDate");
        content.Add(new StringContent(season), "season");
        content.Add(new StringContent(day), "day");
        content.Add(new StringContent(competitionId.ToString(CultureInfo.InvariantCulture)), "competitionId");

        return (await transport.SendContentAsync<AdminImportPreview>(
            HttpMethod.Post,
            "api/v2/admin/imports/preview",
            content,
            cancellationToken: cancellationToken)).Value;
    }

    public async Task<AdminImportExecution> ExecuteAsync(
        Guid previewId,
        AdminImportExecutionRequest request,
        CancellationToken cancellationToken = default) =>
        (await transport.SendJsonAsync<AdminImportExecution>(
            HttpMethod.Post,
            $"api/v2/admin/imports/{previewId:D}/execute",
            request,
            cancellationToken: cancellationToken)).Value;
}

public sealed class AdminMatchApiClient(IAdminApiTransport transport) : IAdminMatchApiClient
{
    public async Task<AdminPageResult<MatchListItemDto>> GetMatchesAsync(
        int page,
        int pageSize,
        string? season,
        string? day,
        int? teamId,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var query = new QueryStringBuilder("api/Matches")
            .Add("page", Math.Max(1, page))
            .Add("pageSize", Math.Clamp(pageSize, 1, 100))
            .Add("season", season)
            .Add("day", day)
            .Add("teamId", teamId)
            .Add("from", from?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
            .Add("to", to?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        var items = (await transport.GetAsync<List<MatchListItemDto>>(
            query.ToString(),
            cancellationToken)).Value;
        return new AdminPageResult<MatchListItemDto>(items, page, pageSize, null, items.Count == pageSize);
    }

    public Task<AdminHttpResult<AdminMatch>> GetMatchAsync(
        int matchId,
        CancellationToken cancellationToken = default) =>
        transport.GetAsync<AdminMatch>($"api/v2/admin/matches/{matchId}", cancellationToken);

    public Task<AdminHttpResult<AdminMatch>> UpdateMatchAsync(
        int matchId,
        AdminMatchUpdate request,
        string etag,
        CancellationToken cancellationToken = default) =>
        transport.SendJsonAsync<AdminMatch>(
            HttpMethod.Put,
            $"api/v2/admin/matches/{matchId}",
            request,
            etag,
            cancellationToken);

    public async Task<AdminDeletionImpact> GetDeletionImpactAsync(
        int matchId,
        CancellationToken cancellationToken = default) =>
        (await transport.GetAsync<AdminDeletionImpact>(
            $"api/v2/admin/matches/{matchId}/deletion-impact",
            cancellationToken)).Value;

    public async Task<AdminLifecycleResult> ArchiveAsync(
        int matchId,
        AdminLifecycleRequest request,
        string etag,
        CancellationToken cancellationToken = default) =>
        (await transport.SendJsonAsync<AdminLifecycleResult>(
            HttpMethod.Delete,
            $"api/v2/admin/matches/{matchId}",
            request,
            etag,
            cancellationToken)).Value;

    public async Task<AdminLifecycleResult> RestoreAsync(
        int matchId,
        AdminLifecycleRequest request,
        string etag,
        CancellationToken cancellationToken = default) =>
        (await transport.SendJsonAsync<AdminLifecycleResult>(
            HttpMethod.Post,
            $"api/v2/admin/matches/{matchId}/restore",
            request,
            etag,
            cancellationToken)).Value;
}

public sealed class AdminEventApiClient(IAdminApiTransport transport) : IAdminEventApiClient
{
    public async Task<IReadOnlyList<LegacyMatchEvent>> GetEventsAsync(
        int matchId,
        CancellationToken cancellationToken = default) =>
        (await transport.GetAsync<List<LegacyMatchEvent>>(
            $"api/MatchEvents?matchId={matchId}",
            cancellationToken)).Value;

    public Task<AdminHttpResult<AdminMatchEvent>> GetEventAsync(
        int matchId,
        int eventId,
        CancellationToken cancellationToken = default) =>
        transport.GetAsync<AdminMatchEvent>(
            $"api/v2/admin/matches/{matchId}/events/{eventId}",
            cancellationToken);

    public Task<AdminHttpResult<AdminMatchEvent>> UpdateEventAsync(
        int matchId,
        int eventId,
        AdminMatchEventUpdate request,
        string etag,
        CancellationToken cancellationToken = default) =>
        transport.SendJsonAsync<AdminMatchEvent>(
            HttpMethod.Put,
            $"api/v2/admin/matches/{matchId}/events/{eventId}",
            request,
            etag,
            cancellationToken);

    public async Task<AdminDeletionImpact> GetDeletionImpactAsync(
        int matchId,
        int eventId,
        CancellationToken cancellationToken = default) =>
        (await transport.GetAsync<AdminDeletionImpact>(
            $"api/v2/admin/matches/{matchId}/events/{eventId}/deletion-impact",
            cancellationToken)).Value;

    public async Task<AdminLifecycleResult> ArchiveAsync(
        int matchId,
        int eventId,
        AdminLifecycleRequest request,
        string etag,
        CancellationToken cancellationToken = default) =>
        (await transport.SendJsonAsync<AdminLifecycleResult>(
            HttpMethod.Delete,
            $"api/v2/admin/matches/{matchId}/events/{eventId}",
            request,
            etag,
            cancellationToken)).Value;

    public async Task<AdminLifecycleResult> RestoreAsync(
        int matchId,
        int eventId,
        AdminLifecycleRequest request,
        string etag,
        CancellationToken cancellationToken = default) =>
        (await transport.SendJsonAsync<AdminLifecycleResult>(
            HttpMethod.Post,
            $"api/v2/admin/matches/{matchId}/events/{eventId}/restore",
            request,
            etag,
            cancellationToken)).Value;
}

public sealed class AdminPlayerApiClient(IAdminApiTransport transport) : IAdminPlayerApiClient
{
    public async Task<AdminPageResult<PlayerListItemDto>> GetPlayersAsync(
        int page,
        int pageSize,
        string? search,
        int? teamId,
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        var query = new QueryStringBuilder("api/Players")
            .Add("page", Math.Max(1, page))
            .Add("pageSize", Math.Clamp(pageSize, 1, 100))
            .Add("search", search)
            .Add("teamId", teamId)
            .Add("isActive", isActive?.ToString().ToLowerInvariant());
        var items = (await transport.GetAsync<List<PlayerListItemDto>>(
            query.ToString(),
            cancellationToken)).Value;
        return new AdminPageResult<PlayerListItemDto>(items, page, pageSize, null, items.Count == pageSize);
    }

    public async Task<PlayerListItemDto> GetPlayerAsync(
        int playerId,
        CancellationToken cancellationToken = default) =>
        (await transport.GetAsync<PlayerListItemDto>($"api/Players/{playerId}", cancellationToken)).Value;

    public async Task<Player> CreatePlayerAsync(
        AdminPlayerMutation request,
        CancellationToken cancellationToken = default)
    {
        var body = new
        {
            Name = request.Name ?? string.Empty,
            Surname = request.Surname ?? string.Empty,
            request.Birthday,
            request.Age,
            request.PositionId,
            request.TeamId,
            request.NationalityId,
            request.Number,
            IsActive = request.IsActive ?? true,
            request.Photo
        };
        return (await transport.SendJsonAsync<Player>(
            HttpMethod.Post,
            "api/Players",
            body,
            cancellationToken: cancellationToken)).Value;
    }

    public async Task<Player> UpdatePlayerAsync(
        int playerId,
        AdminPlayerMutation request,
        CancellationToken cancellationToken = default) =>
        (await transport.SendJsonAsync<Player>(
            HttpMethod.Put,
            $"api/Players/{playerId}",
            request,
            cancellationToken: cancellationToken)).Value;
}

public sealed class AdminTeamApiClient(IAdminApiTransport transport) : IAdminTeamApiClient
{
    public AdminApiAvailability WriteAvailability { get; } = new(
        false,
        "BLOCKED_BY_API",
        "POST/PUT /api/v2/admin/teams",
        "La lecture des equipes est disponible, mais leur administration auditee ne l'est pas.");

    public async Task<IReadOnlyList<TeamDto>> GetTeamsAsync(CancellationToken cancellationToken = default) =>
        (await transport.GetAsync<List<TeamDto>>("api/Teams", cancellationToken)).Value;

    public async Task<TeamDto> GetTeamAsync(int teamId, CancellationToken cancellationToken = default) =>
        (await transport.GetAsync<TeamDto>($"api/Teams/{teamId}", cancellationToken)).Value;
}

public sealed class AdminReferenceDataApiClient(IAdminApiTransport transport) : IAdminReferenceDataApiClient
{
    public AdminApiAvailability WriteAvailability { get; } = new(
        false,
        "BLOCKED_BY_API",
        "POST/PUT /api/v2/admin/reference-data/{catalog}",
        "Les catalogues sont consultables, mais les modifications ne disposent pas encore d'un contrat admin.");

    public async Task<IReadOnlyList<CompetitionDto>> GetCompetitionsAsync(CancellationToken cancellationToken = default) =>
        (await transport.GetAsync<List<CompetitionDto>>("api/Competitions", cancellationToken)).Value;

    public Task<IReadOnlyList<LookupItemDto>> GetEventsAsync(CancellationToken cancellationToken = default) =>
        GetLookupAsync("events", cancellationToken);

    public Task<IReadOnlyList<LookupItemDto>> GetPositionsAsync(CancellationToken cancellationToken = default) =>
        GetLookupAsync("positions", cancellationToken);

    public Task<IReadOnlyList<LookupItemDto>> GetNationalitiesAsync(CancellationToken cancellationToken = default) =>
        GetLookupAsync("nationalities", cancellationToken);

    public Task<IReadOnlyList<LookupItemDto>> GetAttacksAsync(CancellationToken cancellationToken = default) =>
        GetLookupAsync("attacks", cancellationToken);

    public Task<IReadOnlyList<LookupItemDto>> GetDefensesAsync(CancellationToken cancellationToken = default) =>
        GetLookupAsync("defenses", cancellationToken);

    private async Task<IReadOnlyList<LookupItemDto>> GetLookupAsync(
        string catalog,
        CancellationToken cancellationToken) =>
        (await transport.GetAsync<List<LookupItemDto>>(
            $"api/Lookups/{catalog}",
            cancellationToken)).Value;
}

public sealed class AdminDataQualityApiClient : IAdminDataQualityApiClient
{
    public AdminApiAvailability Availability { get; } = new(
        false,
        "BLOCKED_BY_API",
        "GET /api/v2/admin/data-quality/issues",
        "Aucune route de qualite ou de resolution n'est exposee par l'API deployee.");
}

public sealed class AdminAuditApiClient(IAdminApiTransport transport) : IAdminAuditApiClient
{
    public async Task<AdminPageResult<AdminAuditEntry>> GetAuditAsync(
        int page,
        int pageSize,
        string? entityType,
        string? entityId,
        CancellationToken cancellationToken = default)
    {
        var query = new QueryStringBuilder("api/v2/admin/audit")
            .Add("page", Math.Max(1, page))
            .Add("pageSize", Math.Clamp(pageSize, 1, 100))
            .Add("entityType", entityType)
            .Add("entityId", entityId);
        var result = (await transport.GetAsync<AuditPageContract>(query.ToString(), cancellationToken)).Value;
        return new AdminPageResult<AdminAuditEntry>(
            result.Items,
            result.Page,
            result.PageSize,
            result.Total,
            result.Page * result.PageSize < result.Total);
    }

    private sealed record AuditPageContract(
        IReadOnlyList<AdminAuditEntry> Items,
        int Page,
        int PageSize,
        long Total);
}

public sealed class AdminMaintenanceApiClient(IAdminApiTransport transport) : IAdminMaintenanceApiClient
{
    public AdminApiAvailability ActionAvailability { get; } = new(
        false,
        "BLOCKED_BY_API",
        "POST /api/v2/admin/maintenance/{task}",
        "Seuls les diagnostics de version sont disponibles. Aucune commande libre n'est autorisee.");

    public async Task<AdminSystemVersion> GetVersionAsync(CancellationToken cancellationToken = default) =>
        (await transport.GetAsync<AdminSystemVersion>("api/system/version", cancellationToken)).Value;
}

public sealed class AdminUsersApiClient(IAdminApiTransport transport) : IAdminUsersApiClient
{
    public async Task<IReadOnlyList<AdminUser>> GetUsersAsync(CancellationToken cancellationToken = default) =>
        (await transport.GetAsync<List<AdminUser>>("api/Users", cancellationToken)).Value;

    public async Task<AdminUser> CreateUserAsync(
        AdminUserCreate request,
        CancellationToken cancellationToken = default) =>
        (await transport.SendJsonAsync<AdminUser>(
            HttpMethod.Post,
            "api/Users",
            request,
            cancellationToken: cancellationToken)).Value;

    public async Task<AdminUser> UpdateUserAsync(
        int userId,
        AdminUserUpdate request,
        CancellationToken cancellationToken = default) =>
        (await transport.SendJsonAsync<AdminUser>(
            HttpMethod.Put,
            $"api/Users/{userId}",
            request,
            cancellationToken: cancellationToken)).Value;
}

internal sealed class QueryStringBuilder(string path)
{
    private readonly List<string> _parameters = [];

    public QueryStringBuilder Add(string name, object? value)
    {
        if (value is null || value is string text && string.IsNullOrWhiteSpace(text))
        {
            return this;
        }

        _parameters.Add($"{Uri.EscapeDataString(name)}={Uri.EscapeDataString(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty)}");
        return this;
    }

    public override string ToString() =>
        _parameters.Count == 0 ? path : $"{path}?{string.Join('&', _parameters)}";
}
