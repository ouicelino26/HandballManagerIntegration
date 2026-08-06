using System.Net;

namespace HandballIntegration.Core.Models;

public enum AdminPageStateKind
{
    Idle,
    Loading,
    Loaded,
    Empty,
    Partial,
    Error,
    Forbidden,
    Conflict,
    Offline,
    Cancelled
}

public sealed record AdminPageState(
    AdminPageStateKind Kind,
    string Title,
    string Message,
    string? ErrorCode = null,
    string? CorrelationId = null,
    bool CanRetry = false)
{
    public static AdminPageState Idle(string message = "Pret") =>
        new(AdminPageStateKind.Idle, "Pret", message);

    public static AdminPageState Loading(string message = "Chargement en cours") =>
        new(AdminPageStateKind.Loading, "Chargement", message);

    public static AdminPageState Loaded(string message = "Donnees a jour") =>
        new(AdminPageStateKind.Loaded, "A jour", message);

    public static AdminPageState Empty(string message) =>
        new(AdminPageStateKind.Empty, "Aucun resultat", message);

    public static AdminPageState Partial(string message) =>
        new(AdminPageStateKind.Partial, "Donnees partielles", message);

    public static AdminPageState Cancelled() =>
        new(AdminPageStateKind.Cancelled, "Operation annulee", "La requete a ete annulee.");

    public static AdminPageState FromError(HttpStatusCode status, string code, string message, string? correlationId)
    {
        var kind = status switch
        {
            HttpStatusCode.Forbidden => AdminPageStateKind.Forbidden,
            HttpStatusCode.Conflict or HttpStatusCode.PreconditionFailed => AdminPageStateKind.Conflict,
            HttpStatusCode.ServiceUnavailable or HttpStatusCode.BadGateway or HttpStatusCode.GatewayTimeout =>
                AdminPageStateKind.Offline,
            _ => AdminPageStateKind.Error
        };

        return new AdminPageState(kind, StateTitle(kind), message, code, correlationId, CanRetryStatus(status));
    }

    private static bool CanRetryStatus(HttpStatusCode status) =>
        status is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests or
            HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout;

    private static string StateTitle(AdminPageStateKind kind) => kind switch
    {
        AdminPageStateKind.Forbidden => "Acces non autorise",
        AdminPageStateKind.Conflict => "Conflit a resoudre",
        AdminPageStateKind.Offline => "API indisponible",
        _ => "Operation impossible"
    };
}

public sealed record AdminHttpResult<T>(
    T Value,
    string? ETag,
    string? AuditId,
    string? CorrelationId);

public sealed record AdminPageResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    long? Total,
    bool HasNextPage);

public sealed record AdminSystemVersion(
    string ApiVersion,
    string DatabaseVersion,
    string? GitCommitSha,
    IReadOnlyList<AdminComponentVersion> Components);

public sealed record AdminComponentVersion(
    string ComponentCode,
    string Version,
    long BuildNumber,
    string? GitCommitSha);

public sealed record AdminEntityReference(
    string EntityType,
    string EntityId,
    long Version,
    string State);

public sealed record AdminValidationIssue(
    string Code,
    string Message,
    string? Field,
    int? Row,
    string Severity);

public sealed record AdminImpactItem(
    string Category,
    string Description,
    long Count,
    string Severity);

public sealed record AdminDeletionImpact(
    AdminEntityReference Entity,
    long CurrentVersion,
    string CurrentEtag,
    bool CanDelete,
    string RecommendedMode,
    IReadOnlyList<AdminImpactItem> Dependencies,
    IReadOnlyList<string> AffectedStatistics,
    IReadOnlyList<string> AffectedAggregates,
    IReadOnlyList<string> AffectedCaches,
    IReadOnlyList<string> AffectedExports,
    IReadOnlyList<string> AffectedAuditReferences,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> BlockingReasons,
    string ConfirmationToken);

public sealed record AdminLifecycleRequest(
    string Reason,
    long ExpectedVersion,
    string ExpectedEtag,
    string? CorrelationId,
    string ConfirmationToken);

public sealed record AdminLifecycleResult(
    IReadOnlyList<AdminEntityReference>? ArchivedEntities,
    IReadOnlyList<AdminEntityReference>? RestoredEntities,
    IReadOnlyList<string> RecalculatedScopes,
    IReadOnlyList<string> Warnings,
    Guid AuditId,
    long NewVersion,
    string ETag);

public sealed record AdminMatch(
    int MatchId,
    long Version,
    string ETag,
    string State,
    string Availability,
    int? CompetitionId,
    DateTime? Date,
    int? Team1Id,
    int? Team2Id,
    int? Team1Score,
    int? Team2Score,
    int? Year,
    string? Season,
    string? Day,
    DateTime UpdatedAtUtc,
    string? UpdatedBy);

public sealed record AdminMatchUpdate(
    int? CompetitionId,
    DateTime? Date,
    int? Team1Id,
    int? Team2Id,
    int? Team1Score,
    int? Team2Score,
    int? Year,
    string? Season,
    string? Day,
    string Reason);

public sealed record AdminMatchEvent(
    int MatchEventId,
    long Version,
    string ETag,
    string State,
    string Availability,
    int? MatchId,
    int? PlayerId,
    TimeSpan? Time,
    string? Period,
    int? TeamScore1,
    int? TeamScore2,
    int EventId,
    int? TeamId,
    int? AttackId,
    int? DefenseId,
    string? Action,
    bool? Goal,
    DateTime UpdatedAtUtc,
    string? UpdatedBy);

public sealed record AdminMatchEventUpdate(
    int? PlayerId,
    TimeSpan? Time,
    string? Period,
    int? TeamScore1,
    int? TeamScore2,
    int EventId,
    int? TeamId,
    int? AttackId,
    int? DefenseId,
    string? Action,
    string? ShootZone,
    string? Shade,
    string? ShootShade,
    string? ArmSide,
    string? Jump,
    bool? Goal,
    string? Trigger,
    string Reason);

public sealed record AdminImportPreview(
    Guid PreviewId,
    string FileName,
    long FileSize,
    string Sha256,
    string ImportType,
    string MappingVersion,
    string DuplicateStatus,
    AdminEntityReference? MatchCandidate,
    int ProposedCreates,
    int ProposedUpdates,
    int ProposedSkips,
    IReadOnlyList<AdminValidationIssue> BlockingIssues,
    IReadOnlyList<AdminValidationIssue> Warnings,
    DateTime ExpiresAtUtc,
    bool CanExecute,
    string ExpectedSummary);

public sealed record AdminImportExecutionRequest(
    string ExpectedSha256,
    string ExpectedMappingVersion,
    string IdempotencyKey,
    string Reason,
    bool Confirmation,
    string ExpectedSummary);

public sealed record AdminImportLineResult(
    int Row,
    string Status,
    string Availability,
    string? EntityType,
    string? EntityId,
    IReadOnlyList<AdminValidationIssue> Issues);

public sealed record AdminImportExecution(
    Guid ImportExecutionId,
    string Status,
    int Created,
    int Updated,
    int Skipped,
    int Rejected,
    IReadOnlyList<AdminImportLineResult> Issues,
    Guid AuditId,
    DateTime StartedAtUtc,
    DateTime CompletedAtUtc,
    bool RolledBack,
    string CorrelationId);

public sealed record AdminAuditEntry(
    Guid AuditId,
    DateTime TimestampUtc,
    string UserId,
    string UserRole,
    string Permission,
    string Action,
    string EntityType,
    string EntityId,
    string? BeforeJson,
    string? AfterJson,
    string? Reason,
    string CorrelationId,
    string? ClientVersion,
    string ApiVersion,
    string Source,
    bool Success,
    string? ErrorCode);

public sealed record LegacyMatchEvent(
    int Id,
    int? MatchId,
    int? PlayerId,
    int? TeamId,
    int EventId,
    TimeSpan? Time,
    string? MiTemps,
    int? TeamScore1,
    int? TeamScore2,
    string? Action,
    bool? Goal);

public sealed record AdminPlayerMutation(
    string? Name,
    string? Surname,
    DateTime? Birthday,
    int? Age,
    int? PositionId,
    int? TeamId,
    int? NationalityId,
    int? Number,
    bool? IsActive,
    string? Photo);

public sealed record AdminUser(
    int Id,
    string Username,
    string? Email,
    string Role,
    bool IsActive);

public sealed record AdminUserCreate(
    string Username,
    string Password,
    string? Email,
    string Role);

public sealed record AdminUserUpdate(
    string? Email,
    string? Password,
    string? Role,
    bool? IsActive);

public sealed record AdminUserRolesUpdate(string Role, string Reason);

public sealed record AdminUserStatusUpdate(bool IsActive, string Reason);

public sealed record AdminApiAvailability(
    bool IsAvailable,
    string Status,
    string RequiredRoute,
    string Message);

public sealed record AdminMatchListItemDto(
    int MatchId,
    int? CompetitionId,
    string? CompetitionName,
    DateTime? Date,
    int? Team1Id,
    string? Team1Name,
    int? Team2Id,
    string? Team2Name,
    int? Team1Score,
    int? Team2Score,
    string? Season,
    string? Day,
    int? Year,
    string? State,
    int EventCount,
    long Version,
    string ETag,
    DateTime UpdatedAtUtc);

public sealed record AdminEventListItemDto(
    int Id,
    int MatchId,
    int? PlayerId,
    string? PlayerName,
    int? TeamId,
    string? TeamName,
    int EventId,
    string? EventName,
    TimeSpan? Time,
    string? Period,
    int? TeamScore1,
    int? TeamScore2,
    string? Action,
    bool? Goal,
    bool IsDeleted,
    long Version,
    string ETag,
    DateTime UpdatedAtUtc)
{
    /// <summary>Alias used by the events DataGrid (MiTemps binding).</summary>
    public string? MiTemps => Period;
}

public sealed record AdminTeamListItemDto(
    int Id,
    string Name,
    string Code,
    int PlayerCount,
    int MatchCount,
    long Version,
    string ETag)
{
    /// <summary>Alias used by the teams DataGrid (TeamName binding) and ViewModels.</summary>
    public string TeamName => Name;
    /// <summary>Alias used by the teams DataGrid (TeamCode binding).</summary>
    public string TeamCode => Code;
    /// <summary>Alias for Id, for compatibility with ViewModels that used TeamDto.TeamId.</summary>
    public int TeamId => Id;
}

public sealed record AdminUserDto(
    int Id,
    string Username,
    string? Email,
    string Role,
    string Status,
    DateTime? LastLoginAt,
    DateTime CreatedAt,
    DateTime UpdatedAtUtc)
{
    /// <summary>Derived from Status for DataGrid binding compatibility.</summary>
    public bool IsActive => string.Equals(Status, "ACTIVE", StringComparison.OrdinalIgnoreCase);
}

public sealed record AdminImportExecutionSummaryDto(
    Guid ImportExecutionId,
    string IdempotencyKey,
    string Status,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc,
    string UserId,
    string? FileName,
    string? Sha256,
    string? ErrorCode);

public sealed record AdminDashboardDto(
    IReadOnlyList<AdminImportExecutionSummaryDto> RecentImports,
    long FailedImports,
    long MatchesTotal,
    long EventsTotal,
    long PlayersTotal,
    long TeamsTotal,
    string ApiVersion,
    DateTime Timestamp);

public sealed record AdminImportExecutionListItemDto(
    Guid Id,
    string IdempotencyKey,
    string Status,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc,
    int RowsProcessed,
    int RowsImported,
    int RowsFailed,
    string? UserId,
    string? FileName,
    string? Sha256,
    string? ErrorMessage);

public sealed record AdminReferenceItemDto(
    int Id,
    string? Name,
    string? Code,
    int ItemCount);

public sealed record AdminCatalogDto(
    string CatalogKey,
    string DisplayName,
    int ItemCount);

public sealed record AdminPageRequest(
    int Page = 1,
    int PageSize = 50,
    string? Search = null,
    string? OrderBy = null);
