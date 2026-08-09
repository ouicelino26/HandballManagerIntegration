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
    Task<AdminDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
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
    Task<AdminPageResult<AdminMatchListItemDto>> GetMatchesAsync(
        int page,
        int pageSize,
        string? season,
        string? day,
        int? teamId,
        DateTime? from,
        DateTime? to,
        string? search = null,
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
    Task<AdminPageResult<AdminEventListItemDto>> GetEventsAsync(
        int matchId,
        int page = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default);
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
    Task<AdminPageResult<AdminTeamListItemDto>> GetTeamsAsync(
        int page = 1,
        int pageSize = 100,
        string? search = null,
        CancellationToken cancellationToken = default);
    Task<AdminTeamListItemDto> GetTeamAsync(int teamId, CancellationToken cancellationToken = default);
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

public interface IAdminImportHistoryApiClient
{
    Task<AdminPageResult<AdminImportExecutionListItemDto>> GetImportsAsync(
        int page = 1,
        int pageSize = 50,
        string? status = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default);
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
    Task<AdminPageResult<AdminUserDto>> GetUsersAsync(
        int page = 1,
        int pageSize = 100,
        string? search = null,
        CancellationToken cancellationToken = default);
    Task<AdminUserDto> CreateUserAsync(AdminUserCreate request, CancellationToken cancellationToken = default);
    Task<AdminUserDto> UpdateUserAsync(
        int userId,
        AdminUserUpdate request,
        CancellationToken cancellationToken = default);
    Task<AdminUserDto> UpdateRoleAsync(
        int userId,
        string role,
        string reason,
        CancellationToken cancellationToken = default);
    Task<AdminUserDto> UpdateStatusAsync(
        int userId,
        bool isActive,
        string reason,
        CancellationToken cancellationToken = default);
}
