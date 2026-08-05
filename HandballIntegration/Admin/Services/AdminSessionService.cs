using HandballIntegration.Admin.Abstractions;
using HandballIntegration.Admin.Models;

namespace HandballIntegration.Admin.Services;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}

public sealed class CorrelationIdProvider : ICorrelationIdProvider
{
    public string Create() => Guid.NewGuid().ToString("N");
}

public sealed class MemoryAdminSessionStorage : IAdminSessionStorage
{
    private AdminSession? _session;
    public AdminSession? Load() => _session;
    public void Save(AdminSession session) => _session = session;
    public void Clear() => _session = null;
}

public sealed class AdminSessionService(
    IClock clock,
    IAdminSessionStorage storage) : IAdminSessionService
{
    public AdminSession? Current { get; private set; } = storage.Load();
    public bool IsAuthenticated => Current is not null && !IsExpired;
    public bool IsExpired => Current is not null && Current.ExpiresAtUtc <= clock.UtcNow;
    public event EventHandler? SessionChanged;

    public void Set(AdminSession session)
    {
        Current = session;
        storage.Save(session);
        SessionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        Current = null;
        storage.Clear();
        SessionChanged?.Invoke(this, EventArgs.Empty);
    }
}
