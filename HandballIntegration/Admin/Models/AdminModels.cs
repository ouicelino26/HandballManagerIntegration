using System.Net;

namespace HandballIntegration.Admin.Models;

public sealed record AdminSession(
    string AccessToken,
    string Username,
    string Role,
    IReadOnlySet<string> Permissions,
    DateTime ExpiresAtUtc);

public sealed record AdminCapabilityDto(string Name, bool Allowed);

public sealed record AdminCapabilitiesResponse(
    string UserId,
    string Username,
    string Role,
    IReadOnlyList<AdminCapabilityDto> Capabilities,
    string ApiVersion);

public sealed record AdminClientError(
    HttpStatusCode Status,
    string Code,
    string Message,
    string Action,
    bool Retryable,
    string? CorrelationId);

public sealed class AdminApiException(AdminClientError error) : Exception(error.Message)
{
    public AdminClientError Error { get; } = error;
}

public sealed class AdminSessionExpiredException : Exception
{
    public AdminSessionExpiredException() : base("The administrative session expired.") { }
}

public sealed record AdminNavigationItem(
    string Tag,
    string Label,
    string Description,
    string Glyph,
    string RequiredPermission,
    string Status,
    bool IsAvailable);

public static class AdminModuleStatus
{
    public const string FoundationReady = "FOUNDATION_READY";
    public const string ReadOnlyAvailable = "READ_ONLY_AVAILABLE";
    public const string Partial = "PARTIAL";
    public const string Blocked = "BLOCKED";
    public const string NotImplemented = "NOT_IMPLEMENTED";
}

public static class AdminPermissionNames
{
    public const string DashboardRead = "AdminDashboard.Read";
    public const string ImportsRead = "Imports.Read";
    public const string ImportsPreview = "Imports.Preview";
    public const string ImportsExecute = "Imports.Execute";
    public const string MatchesRead = "Matches.Read";
    public const string EventsRead = "Events.Read";
    public const string PlayersRead = "Players.Read";
    public const string TeamsRead = "Teams.Read";
    public const string ReferenceDataManage = "ReferenceData.Manage";
    public const string DataQualityManage = "DataQuality.Manage";
    public const string AuditRead = "Audit.Read";
    public const string UsersManage = "Users.Manage";
}

public enum AdminValueAvailability
{
    Available,
    Partial,
    DataMissing,
    NotApplicable,
    Invalid,
    Conflict
}

public sealed record AdminValue<T>(T? Value, AdminValueAvailability Availability)
    where T : struct
{
    public static AdminValue<T> Missing() => new(default, AdminValueAvailability.DataMissing);
    public static AdminValue<T> Available(T value) => new(value, AdminValueAvailability.Available);
}
