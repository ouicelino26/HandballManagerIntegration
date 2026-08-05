# Admin Navigation — Final State

Generated: 2026-08-06  
Branch: feature/handwstat-admin-product-final-v1

---

## Navigation Structure

```
AdminShell (dark green sidebar)
├── Dashboard          → DashboardPage (API status + player metrics)
├── Matches            → MatchesPage (list + events subview)
├── Players            → PlayersAdminPage (list + inline edit)
├── Teams              → TeamsAdminPage (list + details)
├── Import             → ImportPage (preview → execute workflow)
├── Reference Data     → ReferenceDataPage (catalog browse + edit)
├── Users              → UsersAdminPage (list + create + edit)
└── Audit              → AuditPage (read-only audit trail)
```

---

## Route→View Mapping

| Nav Item | API Route | View | ViewModel |
|---|---|---|---|
| Dashboard | /api/v2/admin/dashboard | DashboardPage.xaml | DashboardPage.xaml.cs |
| Matches | /api/v2/admin/matches | MatchesView | MatchesViewModel |
| Match Events | /api/v2/admin/matches/{id}/events | (inline) | MatchesViewModel |
| Players | /api/v2/admin/players | PlayersPage.xaml | PlayersAdminViewModel |
| Teams | /api/v2/admin/teams | (TeamsView) | TeamsAdminViewModel |
| Import | /api/v2/admin/imports/preview + execute | ImportPage | TimeIntegrationViewModel |
| Import History | /api/v2/admin/imports | (imports list) | AdminImportHistoryViewModel |
| Reference Data | /api/v2/admin/reference-data/{catalog} | (ref data view) | (ref data VM) |
| Users | /api/v2/admin/users | (users view) | UsersAdminViewModel |
| Audit | /api/v2/admin/audit | (audit view) | (audit VM) |

---

## Shell Components

- **AdminShellWindow**: top-level Window with sidebar ListBox + Frame content area
- **NavItemStyle** (Navigation.xaml): full-width ListBoxItem with dark green hover/selected states
- **NavGroupHeaderStyle**: uppercase section dividers
- **NavBadgeStyle**: right-aligned count badges for pending items

---

## State Propagation

Each page ViewModel exposes:
- `IsBusy` (bool): drives loading overlay
- `ErrorMessage` (string?): drives error panel
- `IsEmpty` (bool): drives empty state placeholder
- `IsForbidden` (bool): drives 403 forbidden panel

Pages bind to these properties to show the appropriate state view without code-behind state logic.
