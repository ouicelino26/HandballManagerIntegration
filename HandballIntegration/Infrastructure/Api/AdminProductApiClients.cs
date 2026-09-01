using System.Globalization;
using System.IO;
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

    public async Task<AdminDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default) =>
        (await transport.GetAsync<AdminDashboardDto>("api/v2/admin/dashboard", cancellationToken)).Value;
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
    public async Task<AdminPageResult<AdminMatchListItemDto>> GetMatchesAsync(
        int page,
        int pageSize,
        string? season,
        string? day,
        int? teamId,
        DateTime? from,
        DateTime? to,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = new QueryStringBuilder("api/v2/admin/matches")
            .Add("page", Math.Max(1, page))
            .Add("pageSize", Math.Clamp(pageSize, 1, 100))
            .Add("season", season)
            .Add("day", day)
            .Add("teamId", teamId)
            .Add("from", from?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
            .Add("to", to?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
            .Add("search", search);
        var result = (await transport.GetAsync<V2PageContract<AdminMatchListItemDto>>(
            query.ToString(), cancellationToken)).Value;
        return new AdminPageResult<AdminMatchListItemDto>(
            result.Items,
            result.Page,
            result.PageSize,
            result.Total,
            result.Page * result.PageSize < result.Total);
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
    public async Task<AdminPageResult<AdminEventListItemDto>> GetEventsAsync(
        int matchId,
        int page = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        var query = new QueryStringBuilder($"api/v2/admin/matches/{matchId}/events")
            .Add("page", Math.Max(1, page))
            .Add("pageSize", Math.Clamp(pageSize, 1, 200));
        var result = (await transport.GetAsync<V2PageContract<AdminEventListItemDto>>(
            query.ToString(), cancellationToken)).Value;
        return new AdminPageResult<AdminEventListItemDto>(
            result.Items,
            result.Page,
            result.PageSize,
            result.Total,
            result.Page * result.PageSize < result.Total);
    }

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
        var query = new QueryStringBuilder("api/v2/admin/players")
            .Add("page", Math.Max(1, page))
            .Add("pageSize", Math.Clamp(pageSize, 1, 100))
            .Add("search", search)
            .Add("teamId", teamId)
            .Add("isActive", isActive?.ToString().ToLowerInvariant());
        var result = (await transport.GetAsync<V2PageContract<PlayerListItemDto>>(
            query.ToString(),
            cancellationToken)).Value;
        return new AdminPageResult<PlayerListItemDto>(
            result.Items,
            result.Page,
            result.PageSize,
            result.Total,
            result.Page * result.PageSize < result.Total);
    }

    public async Task<PlayerListItemDto> GetPlayerAsync(
        int playerId,
        CancellationToken cancellationToken = default) =>
        (await transport.GetAsync<PlayerListItemDto>($"api/v2/admin/players/{playerId}", cancellationToken)).Value;

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
            "api/v2/admin/players",
            body,
            cancellationToken: cancellationToken)).Value;
    }

    public async Task<Player> UpdatePlayerAsync(
        int playerId,
        AdminPlayerMutation request,
        CancellationToken cancellationToken = default) =>
        (await transport.SendJsonAsync<Player>(
            HttpMethod.Put,
            $"api/v2/admin/players/{playerId}",
            request,
            cancellationToken: cancellationToken)).Value;

    public async Task<PlayerMergeResult> MergePlayerAsync(
        int targetId,
        int sourcePlayerId,
        CancellationToken cancellationToken = default) =>
        (await transport.SendJsonAsync<PlayerMergeResult>(
            HttpMethod.Post,
            $"api/v2/admin/players/{targetId}/merge",
            new { SourcePlayerId = sourcePlayerId },
            cancellationToken: cancellationToken)).Value;
}

public sealed class AdminTeamApiClient(IAdminApiTransport transport) : IAdminTeamApiClient
{
    public AdminApiAvailability WriteAvailability { get; } = new(
        false,
        "BLOCKED_BY_API",
        "POST/PUT /api/v2/admin/teams",
        "La lecture des equipes est disponible, mais leur administration auditee ne l'est pas.");

    public async Task<AdminPageResult<AdminTeamListItemDto>> GetTeamsAsync(
        int page = 1,
        int pageSize = 100,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = new QueryStringBuilder("api/v2/admin/teams")
            .Add("page", Math.Max(1, page))
            .Add("pageSize", Math.Clamp(pageSize, 1, 200))
            .Add("search", search);
        var result = (await transport.GetAsync<V2PageContract<AdminTeamListItemDto>>(
            query.ToString(), cancellationToken)).Value;
        return new AdminPageResult<AdminTeamListItemDto>(
            result.Items,
            result.Page,
            result.PageSize,
            result.Total,
            result.Page * result.PageSize < result.Total);
    }

    public async Task<AdminTeamListItemDto> GetTeamAsync(int teamId, CancellationToken cancellationToken = default) =>
        (await transport.GetAsync<AdminTeamListItemDto>($"api/v2/admin/teams/{teamId}", cancellationToken)).Value;
}

public sealed class AdminReferenceDataApiClient(IAdminApiTransport transport) : IAdminReferenceDataApiClient
{
    public AdminApiAvailability WriteAvailability { get; } = new(
        false,
        "BLOCKED_BY_API",
        "POST/PUT /api/v2/admin/reference-data/{catalog}",
        "Les catalogues sont consultables, mais les modifications ne disposent pas encore d'un contrat admin.");

    public async Task<IReadOnlyList<CompetitionDto>> GetCompetitionsAsync(CancellationToken cancellationToken = default)
    {
        var all = await FetchAllPagesAsync("competitions", cancellationToken);
        return all.Select(r => new CompetitionDto
        {
            CompetitionId = r.Id,
            CompetitionCode = r.Code,
            CompetitionName = r.Name,
            MatchCount = r.ItemCount
        }).ToList();
    }

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
        CancellationToken cancellationToken)
    {
        var all = await FetchAllPagesAsync(catalog, cancellationToken);
        return all.Select(r => new LookupItemDto
        {
            Id = r.Id,
            Code = r.Code,
            Name = r.Name
        }).ToList();
    }

    private async Task<List<AdminReferenceItemDto>> FetchAllPagesAsync(
        string catalog,
        CancellationToken cancellationToken)
    {
        const int batchSize = 200;
        var result = new List<AdminReferenceItemDto>();
        var page = 1;
        while (true)
        {
            var url = $"api/v2/admin/reference-data/{catalog}?page={page}&pageSize={batchSize}";
            var batch = (await transport.GetAsync<V2PageContract<AdminReferenceItemDto>>(url, cancellationToken)).Value;
            result.AddRange(batch.Items);
            if (result.Count >= batch.Total || batch.Items.Count < batchSize)
                break;
            page++;
        }
        return result;
    }
}

public sealed class AdminImportHistoryApiClient(IAdminApiTransport transport) : IAdminImportHistoryApiClient
{
    public async Task<AdminPageResult<AdminImportExecutionListItemDto>> GetImportsAsync(
        int page = 1,
        int pageSize = 50,
        string? status = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var query = new QueryStringBuilder("api/v2/admin/imports")
            .Add("page", Math.Max(1, page))
            .Add("pageSize", Math.Clamp(pageSize, 1, 200))
            .Add("status", status)
            .Add("from", from?.ToString("o", System.Globalization.CultureInfo.InvariantCulture))
            .Add("to", to?.ToString("o", System.Globalization.CultureInfo.InvariantCulture));
        var result = (await transport.GetAsync<V2PageContract<AdminImportExecutionListItemDto>>(
            query.ToString(), cancellationToken)).Value;
        return new AdminPageResult<AdminImportExecutionListItemDto>(
            result.Items, result.Page, result.PageSize, result.Total,
            result.Page * result.PageSize < result.Total);
    }
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
    public async Task<AdminPageResult<AdminUserDto>> GetUsersAsync(
        int page = 1,
        int pageSize = 100,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = new QueryStringBuilder("api/v2/admin/users")
            .Add("page", Math.Max(1, page))
            .Add("pageSize", Math.Clamp(pageSize, 1, 200))
            .Add("search", search);
        var result = (await transport.GetAsync<V2PageContract<AdminUserDto>>(
            query.ToString(), cancellationToken)).Value;
        return new AdminPageResult<AdminUserDto>(
            result.Items,
            result.Page,
            result.PageSize,
            result.Total,
            result.Page * result.PageSize < result.Total);
    }

    public async Task<AdminUserDto> CreateUserAsync(
        AdminUserCreate request,
        CancellationToken cancellationToken = default) =>
        (await transport.SendJsonAsync<AdminUserDto>(
            HttpMethod.Post,
            "api/v2/admin/users",
            request,
            cancellationToken: cancellationToken)).Value;

    public async Task<AdminUserDto> UpdateUserAsync(
        int userId,
        AdminUserUpdate request,
        CancellationToken cancellationToken = default) =>
        (await transport.SendJsonAsync<AdminUserDto>(
            HttpMethod.Put,
            $"api/v2/admin/users/{userId}",
            new { Email = request.Email, Reason = "Mise a jour du compte" },
            cancellationToken: cancellationToken)).Value;

    public async Task<AdminUserDto> UpdateRoleAsync(
        int userId,
        string role,
        string reason,
        CancellationToken cancellationToken = default) =>
        (await transport.SendJsonAsync<AdminUserDto>(
            HttpMethod.Put,
            $"api/v2/admin/users/{userId}/roles",
            new AdminUserRolesUpdate(role, reason),
            cancellationToken: cancellationToken)).Value;

    public async Task<AdminUserDto> UpdateStatusAsync(
        int userId,
        bool isActive,
        string reason,
        CancellationToken cancellationToken = default) =>
        (await transport.SendJsonAsync<AdminUserDto>(
            HttpMethod.Put,
            $"api/v2/admin/users/{userId}/status",
            new AdminUserStatusUpdate(isActive, reason),
            cancellationToken: cancellationToken)).Value;
}

internal sealed record V2PageContract<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    long Total);

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
