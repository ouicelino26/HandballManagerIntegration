# WPF test strategy

## Scope

`HandballManagerIntegration.Tests` targets the same Windows framework as the application but tests windowless administrative state and services. This keeps the P0 suite deterministic on developer machines and `windows-latest` CI runners.

The suite currently contains 22 tests covering:

- startup cancellation and safe login errors;
- JWT expiration, 401 logout, and 403 session preservation;
- API-capability navigation and denial of unauthorized modules;
- safe ProblemDetails and correlation identifiers;
- import preview/confirmation separation;
- impact loading, dependencies, and mandatory reason;
- optimistic-concurrency recovery choices;
- preservation of missing imported values;
- cancellable loading and shell environment/version state;
- absence of a client-secret property.

## Commands

```powershell
dotnet restore HandballManagerIntegration.sln
dotnet build HandballManagerIntegration.sln -c Release --no-restore
dotnet test HandballManagerIntegration.Tests/HandballManagerIntegration.Tests.csproj -c Release --no-build
./scripts/scan-secrets.ps1
```

## CI contract

The Windows workflow checks out Integration and its Core dependency side by side, restores, builds Release, runs tests, and scans tracked plus untracked non-ignored files. It performs no packaging, release, database operation, or deployment.

## Later test layers

Phase C should add STA component tests, UI automation for focus/dialog behavior, and authenticated contract tests against an isolated test API. Those layers must use disposable data and must not target production.
