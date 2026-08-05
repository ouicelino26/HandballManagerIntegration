using System.Net;
using HandballIntegration.Admin.Abstractions;
using HandballIntegration.Admin.Models;
using HandballIntegration.Admin.Services;
using HandballIntegration.Admin.Workflows;
using HandballIntegration.Data;

namespace HandballManagerIntegration.Tests;

public sealed class AdminClientFoundationTests
{
    [Fact]
    public void AppStartup_WhenLoginCancelled_ShutsDown()
    {
        Assert.True(AdminStartupDecision.ShouldShutdownAfterLogin(false));
        Assert.True(AdminStartupDecision.ShouldShutdownAfterLogin(null));
        Assert.False(AdminStartupDecision.ShouldShutdownAfterLogin(true));
    }

    [Fact]
    public void Login_InvalidCredentials_DisplaysSafeError()
    {
        var mapper = new AdminProblemDetailsMapper();
        var raw = "{\"detail\":\"database stack trace\",\"code\":\"ADMIN_UNAUTHORIZED\"}";

        var error = mapper.Map(HttpStatusCode.Unauthorized, raw);

        Assert.Equal("ADMIN_UNAUTHORIZED", error.Code);
        Assert.DoesNotContain("database", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("stack", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_ExpiredToken_ReturnsToLogin()
    {
        var clock = new FakeClock(new DateTime(2026, 8, 5, 12, 0, 0, DateTimeKind.Utc));
        var session = CreateSessionService(clock, expiresAt: clock.UtcNow.AddMinutes(-1));
        var handler = new AdminSessionHandler(session) { InnerHandler = new StaticHandler(HttpStatusCode.OK) };
        using var client = new HttpClient(handler);

        await Assert.ThrowsAsync<AdminSessionExpiredException>(() => client.GetAsync("https://example.test"));
        Assert.Null(session.Current);
    }

    [Fact]
    public void UnauthorizedModule_IsNotAccessible()
    {
        var navigation = new AdminNavigationService();
        var modules = navigation.Build(new HashSet<string> { AdminPermissionNames.DashboardRead });

        Assert.DoesNotContain(modules, item => item.Tag == "users");
        Assert.DoesNotContain(modules, item => item.Tag == "integration");
    }

    [Fact]
    public void Navigation_AllowsAuthorizedModule()
    {
        var navigation = new AdminNavigationService();
        var permissions = new HashSet<string> { AdminPermissionNames.ImportsRead };

        Assert.True(navigation.CanAccess("integration", permissions));
        Assert.Contains(navigation.Build(permissions), item => item.Tag == "integration");
    }

    [Fact]
    public void Navigation_UsesApiCapabilities()
    {
        var navigation = new AdminNavigationService();
        var capabilities = new HashSet<string> { AdminPermissionNames.AuditRead };

        var modules = navigation.Build(capabilities);

        Assert.Single(modules);
        Assert.Equal("audit", modules[0].Tag);
    }

    [Fact]
    public void Navigation_DoesNotTrustLocalRoleOnly()
    {
        var navigation = new AdminNavigationService();
        IReadOnlySet<string> noServerCapabilities = new HashSet<string>();

        Assert.False(navigation.CanAccess("users", noServerCapabilities));
        Assert.Empty(navigation.Build(noServerCapabilities));
    }

    [Fact]
    public void PermissionDeniedState_IsDisplayed()
    {
        var state = AdminPermissionViewState.FromCapability(allowed: false);

        Assert.True(state.ShowPermissionDenied);
        Assert.False(state.ShowContent);
    }

    [Fact]
    public void ApiProblemDetails_IsMapped()
    {
        var mapper = new AdminProblemDetailsMapper();
        var error = mapper.Map(
            HttpStatusCode.PreconditionFailed,
            "{\"code\":\"ADMIN_PRECONDITION_FAILED\",\"correlationId\":\"corr-1\"}");

        Assert.Contains("modifiee", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Actualisez", error.Action, StringComparison.Ordinal);
        Assert.Equal("corr-1", error.CorrelationId);
    }

    [Fact]
    public void RawException_IsNotDisplayed()
    {
        var mapper = new AdminProblemDetailsMapper();
        var error = mapper.Map(
            HttpStatusCode.InternalServerError,
            "System.InvalidOperationException at Secret.Internal.Path");

        Assert.DoesNotContain("InvalidOperationException", error.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("Secret.Internal.Path", error.Action, StringComparison.Ordinal);
    }

    [Fact]
    public void ImportPreviewViewModel_DoesNotExecute()
    {
        var workflow = new AdminImportWorkflow();
        workflow.LoadPreview("preview-1", canExecute: true);

        Assert.False(workflow.TryBeginExecute());
    }

    [Fact]
    public void ImportExecuteViewModel_RequiresConfirmation()
    {
        var workflow = new AdminImportWorkflow();
        workflow.LoadPreview("preview-1", canExecute: true);
        workflow.Confirm();

        Assert.True(workflow.TryBeginExecute());
    }

    [Fact]
    public void ImpactDialog_RequiresReason()
    {
        var guard = new AdminDeletionGuard();
        Assert.False(guard.CanSubmit("reason"));

        guard.LoadImpact("confirmation");
        Assert.False(guard.CanSubmit(" "));
        Assert.True(guard.CanSubmit("validated reason"));
    }

    [Fact]
    public void ImpactDialog_DisplaysDependencies()
    {
        var guard = new AdminDeletionGuard();

        guard.LoadImpact("confirmation", ["12 evenements", "2 temps de jeu"]);

        Assert.Equal(2, guard.Dependencies.Count);
        Assert.Contains("12 evenements", guard.Dependencies);
    }

    [Fact]
    public void ConcurrencyDialog_OffersRefresh()
    {
        var choice = new AdminConcurrencyChoice();

        Assert.Contains("Actualiser", choice.Actions);
        Assert.Contains("Comparer", choice.Actions);
        Assert.Contains("Abandonner", choice.Actions);
        Assert.Contains("Reappliquer manuellement", choice.Actions);
    }

    [Fact]
    public void MissingImportedValue_RemainsMissing()
    {
        var value = AdminValue<int>.Missing();

        Assert.Equal(AdminValueAvailability.DataMissing, value.Availability);
        Assert.Null(value.Value);
    }

    [Fact]
    public async Task Session401_ReturnsToLogin()
    {
        var clock = new FakeClock(DateTime.UtcNow);
        var session = CreateSessionService(clock, clock.UtcNow.AddMinutes(5));
        var handler = new AdminSessionHandler(session)
        {
            InnerHandler = new StaticHandler(HttpStatusCode.Unauthorized)
        };
        using var client = new HttpClient(handler);

        using var response = await client.GetAsync("https://example.test");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(session.Current);
    }

    [Fact]
    public async Task Session403_KeepsSessionAndDisplaysForbidden()
    {
        var clock = new FakeClock(DateTime.UtcNow);
        var session = CreateSessionService(clock, clock.UtcNow.AddMinutes(5));
        var handler = new AdminSessionHandler(session)
        {
            InnerHandler = new StaticHandler(HttpStatusCode.Forbidden)
        };
        using var client = new HttpClient(handler);

        using var response = await client.GetAsync("https://example.test");
        var error = new AdminProblemDetailsMapper().Map(response.StatusCode, null);

        Assert.NotNull(session.Current);
        Assert.Equal("ADMIN_FORBIDDEN", error.Code);
    }

    [Fact]
    public void Shell_DoesNotExposeClientSecret()
    {
        Assert.DoesNotContain(
            typeof(ApiSettings).GetProperties(),
            property => property.Name.Contains("Secret", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Shell_ShowsEnvironmentAndApiStatus()
    {
        var state = new AdminShellState("HandWStat Administration", "Test", "Disponible", "1.0", "2.0");

        Assert.Equal("Test", state.Environment);
        Assert.Equal("Disponible", state.ApiStatus);
        Assert.Equal("2.0", state.ApiVersion);
    }

    [Fact]
    public void LoadingState_CanBeCancelled()
    {
        var state = new CancellableLoadingState();
        var token = state.Begin();

        state.Cancel();

        Assert.True(token.IsCancellationRequested);
        Assert.False(state.IsLoading);
    }

    [Fact]
    public void ErrorState_PreservesCorrelationId()
    {
        var error = new AdminProblemDetailsMapper().Map(
            HttpStatusCode.Conflict,
            "{\"code\":\"ADMIN_DEPENDENCY_CONFLICT\",\"correlationId\":\"corr-safe-42\"}");

        var state = AdminErrorState.From(error, new DateTime(2026, 8, 5, 12, 0, 0, DateTimeKind.Utc));

        Assert.Equal("corr-safe-42", state.CorrelationId);
        Assert.DoesNotContain("stack", state.Action, StringComparison.OrdinalIgnoreCase);
    }

    private static AdminSessionService CreateSessionService(FakeClock clock, DateTime expiresAt)
    {
        var service = new AdminSessionService(clock, new MemoryAdminSessionStorage());
        service.Set(new AdminSession(
            "test-token",
            "tester",
            "VIEWER",
            new HashSet<string>(),
            expiresAt));
        return service;
    }

    private sealed class FakeClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
    }

    private sealed class StaticHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(statusCode));
    }
}
