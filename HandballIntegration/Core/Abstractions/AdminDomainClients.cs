using HandballIntegration.Core.Models;
using HandballManagerCore.DTO;
using HandballManagerCore.Models;
using System.Net.Http;

namespace HandballIntegration.Core.Abstractions;

public interface IAdminApiTransport
{
    Task<AdminHttpResult<T>> GetAsync<T>(string relativeUri, CancellationToken cancellationToken = default);
    Task<AdminHttpResult<T>> SendJsonAsync<T>(
        HttpMethod method,
        string relativeUri,
        object body,
        string? ifMatch = null,
        CancellationToken cancellationToken = default);
    Task<AdminHttpResult<T>> SendContentAsync<T>(
        HttpMethod method,
        string relativeUri,
        HttpContent content,
        string? ifMatch = null,
        CancellationToken cancellationToken = default);
}

public interface IAdminDashboardApiClient
{
    Task<AdminSystemVersion> GetVersionAsync(CancellationToken cancellationToken = default);
}

public interface IAdminImportApiClient
{
    Task<AdminImportPreview> PreviewAsync(
        string filePath,
        DateTime matchDate,
        string season,
        string day,
        int competitionId,
        CancellationToken cancellationToken = default);
    Task<AdminImportExecution> ExecuteAsync(
        Guid previewId,
        AdminImportExecutionRequest request,
        CancellationToken cancellationToken = default);
}

public interface IAdminMatchApiClient
{
    Task<AdminPageResult<MatchListItemDto>> GetMatchesAsync(
        int page,
        int pageSize,
        string? season,
        string? day,
        int? teamId,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default);
    Task<AdminHttpResult<AdminMatch>> GetMatchAsync(int matchId, CancellationToken cancellationToken = default);
    Task<AdminHttpResult<AdminMatch>> UpdateMatchAsync(
        int matchId,
        AdminMatchUpdate request,
        string etag,
        CancellationToken cancellationToken = default);
    Task<AdminDeletionImpact> GetDeletionImpactAsync(int matchId, CancellationToken cancellationToken = default);
    Task<AdminLifecycleResult> ArchiveAsync(
        int matchId,
        AdminLifecycleRequest request,
        string etag,
        CancellationToken cancellationToken = default);
    Task<AdminLifecycleResult> RestoreAsync(
        int matchId,
        AdminLifecycleRequest request,
        string etag,
        CancellationToken cancellationToken = default);
}

public interface IAdminEventApiClient
{
    Task<IReadOnlyList<LegacyMatchEvent>> GetEventsAsync(int matchId, CancellationToken cancellationToken = default);
    Task<AdminHttpResult<AdminMatchEvent>> GetEventAsync(
        int matchId,
        int eventId,
        CancellationToken cancellationToken = default);
    Task<AdminHttpResult<AdminMatchEvent>> UpdateEventAsync(
        int matchId,
        int eventId,
        AdminMatchEventUpdate request,
        string etag,
        CancellationToken cancellationToken = default);
    Task<AdminDeletionImpact> GetDeletionImpactAsync(
        int matchId,
        int eventId,
        CancellationToken cancellationToken = default);
    Task<AdminLifecycleResult> ArchiveAsync(
        int matchId,
        int eventId,
        AdminLifecycleRequest request,
        string etag,
        CancellationToken cancellationToken = default);
    Task<AdminLifecycleResult> RestoreAsync(
        int matchId,
        int eventId,
        AdminLifecycleRequest request,
        string etag,
        CancellationToken cancellationToken = default);
}

public interface IAdminPlayerApiClient
{
    Task<AdminPageResult<PlayerListItemDto>> GetPlayersAsync(
        int page,
        int pageSize,
        string? search,
        int? teamId,
        bool? isActive,
        CancellationToken cancellationToken = default);
    Task<PlayerListItemDto> GetPlayerAsync(int playerId, CancellationToken cancellationToken = default);
    Task<Player> CreatePlayerAsync(AdminPlayerMutation request, CancellationToken cancellationToken = default);
    Task<Player> UpdatePlayerAsync(
        int playerId,
        AdminPlayerMutation request,
        CancellationToken cancellationToken = default);
}

public interface IAdminTeamApiClient
{
    Task<IReadOnlyList<TeamDto>> GetTeamsAsync(CancellationToken cancellationToken = default);
    Task<TeamDto> GetTeamAsync(int teamId, CancellationToken cancellationToken = default);
    AdminApiAvailability WriteAvailability { get; }
}

public interface IAdminReferenceDataApiClient
{
    Task<IReadOnlyList<CompetitionDto>> GetCompetitionsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LookupItemDto>> GetEventsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LookupItemDto>> GetPositionsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LookupItemDto>> GetNationalitiesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LookupItemDto>> GetAttacksAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LookupItemDto>> GetDefensesAsync(CancellationToken cancellationToken = default);
    AdminApiAvailability WriteAvailability { get; }
}

public interface IAdminDataQualityApiClient
{
    AdminApiAvailability Availability { get; }
}

public interface IAdminAuditApiClient
{
    Task<AdminPageResult<AdminAuditEntry>> GetAuditAsync(
        int page,
        int pageSize,
        string? entityType,
        string? entityId,
        CancellationToken cancellationToken = default);
}

public interface IAdminMaintenanceApiClient
{
    Task<AdminSystemVersion> GetVersionAsync(CancellationToken cancellationToken = default);
    AdminApiAvailability ActionAvailability { get; }
}

public interface IAdminUsersApiClient
{
    Task<IReadOnlyList<AdminUser>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task<AdminUser> CreateUserAsync(AdminUserCreate request, CancellationToken cancellationToken = default);
    Task<AdminUser> UpdateUserAsync(
        int userId,
        AdminUserUpdate request,
        CancellationToken cancellationToken = default);
}
