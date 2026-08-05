# Live API Route Matrix

Generated: 2026-08-05  
Deployed API: https://handballwstat.ddnsfree.com  
Deployed version: 1.0.0 (database: 1.0.0, commit: null)  
Local API branch: feature/admin-product-completion-v1  

## Legend

| Status | Meaning |
|--------|---------|
| AVAILABLE | Route exists in deployed API |
| MISSING | Route not in deployed API, exists/will exist in local branch |
| LEGACY_ONLY | Only legacy (non-v2) route available |
| NOT_REQUIRED | Route not needed for this mission scope |

---

## Admin v2 Routes

| METHOD | ROUTE | DEPLOYED | LOCAL | CLIENT_USED | PERMISSION | PAGINATED | ETAG | IF_MATCH | AUDIT | TRANSACTION | STATUS | REPLACEMENT_REQUIRED |
|--------|-------|----------|-------|-------------|-----------|-----------|------|----------|-------|-------------|--------|----------------------|
| GET | /api/v2/admin/capabilities | YES | YES | YES | Public | NO | NO | NO | NO | NO | AVAILABLE | NO |
| GET | /api/v2/admin/audit | YES | YES | YES | Audit.Read | YES | NO | NO | NO | NO | AVAILABLE | NO |
| POST | /api/v2/admin/imports/preview | YES | YES | YES | Imports.Preview | NO | NO | NO | YES | YES | AVAILABLE | NO |
| POST | /api/v2/admin/imports/{previewId}/execute | YES | YES | YES | Imports.Execute | NO | NO | NO | YES | YES | AVAILABLE | NO |
| GET | /api/v2/admin/matches | NO | YES | YES (uses legacy) | Matches.Read | YES | NO | NO | NO | NO | MISSING | YES |
| GET | /api/v2/admin/matches/{id} | YES | YES | YES | Matches.Read | NO | YES | NO | NO | NO | AVAILABLE | NO |
| PUT | /api/v2/admin/matches/{id} | YES | YES | YES | Matches.Update | NO | YES | YES | YES | YES | AVAILABLE | NO |
| GET | /api/v2/admin/matches/{id}/deletion-impact | YES | YES | YES | Matches.Read | NO | NO | NO | NO | NO | AVAILABLE | NO |
| DELETE | /api/v2/admin/matches/{id} | YES | YES | YES | Matches.Archive | NO | YES | YES | YES | YES | AVAILABLE | NO |
| POST | /api/v2/admin/matches/{id}/restore | YES | YES | YES | Matches.Restore | NO | YES | YES | YES | YES | AVAILABLE | NO |
| POST | /api/v2/admin/matches/{id}/validate | NO | YES | NO | Matches.Read | NO | NO | NO | NO | NO | MISSING | NO |
| POST | /api/v2/admin/matches/{id}/recalculate | NO | YES | NO | Matches.Recalculate | NO | YES | YES | YES | YES | MISSING | NO |
| GET | /api/v2/admin/matches/{matchId}/events | NO | YES | YES (uses legacy) | Matches.Read | YES | NO | NO | NO | NO | MISSING | YES |
| GET | /api/v2/admin/matches/{matchId}/events/{eventId} | YES | YES | YES | Matches.Read | NO | YES | NO | NO | NO | AVAILABLE | NO |
| PUT | /api/v2/admin/matches/{matchId}/events/{eventId} | YES | YES | YES | Matches.Update | NO | YES | YES | YES | YES | AVAILABLE | NO |
| GET | /api/v2/admin/matches/{matchId}/events/{eventId}/deletion-impact | YES | YES | YES | Matches.Read | NO | NO | NO | NO | NO | AVAILABLE | NO |
| DELETE | /api/v2/admin/matches/{matchId}/events/{eventId} | YES | YES | YES | Matches.Archive | NO | YES | YES | YES | YES | AVAILABLE | NO |
| POST | /api/v2/admin/matches/{matchId}/events/{eventId}/restore | YES | YES | YES | Matches.Restore | NO | YES | YES | YES | YES | AVAILABLE | NO |
| POST | /api/v2/admin/matches/{matchId}/events | NO | YES | NO | Matches.Update | NO | YES | YES | YES | YES | MISSING | NO |
| GET | /api/v2/admin/players | NO | YES | YES (uses legacy) | Players.Read | YES | NO | NO | NO | NO | MISSING | YES |
| GET | /api/v2/admin/players/{id} | NO | YES | YES (uses legacy) | Players.Read | NO | YES | NO | NO | NO | MISSING | YES |
| POST | /api/v2/admin/players | NO | YES | YES (uses legacy) | Players.Create | NO | NO | NO | YES | YES | MISSING | YES |
| PUT | /api/v2/admin/players/{id} | NO | YES | YES (uses legacy) | Players.Update | NO | YES | YES | YES | YES | MISSING | YES |
| GET | /api/v2/admin/players/{id}/deletion-impact | NO | YES | NO | Players.Read | NO | NO | NO | NO | NO | MISSING | NO |
| DELETE | /api/v2/admin/players/{id} | NO | YES | NO | Players.Archive | NO | YES | YES | YES | YES | MISSING | NO |
| POST | /api/v2/admin/players/{id}/restore | NO | YES | NO | Players.Restore | NO | YES | YES | YES | YES | MISSING | NO |
| GET | /api/v2/admin/teams | NO | YES | YES (uses legacy) | Teams.Read | YES | NO | NO | NO | NO | MISSING | YES |
| GET | /api/v2/admin/teams/{id} | NO | YES | YES (uses legacy) | Teams.Read | NO | YES | NO | NO | NO | MISSING | YES |
| POST | /api/v2/admin/teams | NO | YES | NO | Teams.Create | NO | NO | NO | YES | YES | MISSING | NO |
| PUT | /api/v2/admin/teams/{id} | NO | YES | NO | Teams.Update | NO | YES | YES | YES | YES | MISSING | NO |
| GET | /api/v2/admin/teams/{id}/deletion-impact | NO | YES | NO | Teams.Read | NO | NO | NO | NO | NO | MISSING | NO |
| DELETE | /api/v2/admin/teams/{id} | NO | YES | NO | Teams.Archive | NO | YES | YES | YES | YES | MISSING | NO |
| POST | /api/v2/admin/teams/{id}/restore | NO | YES | NO | Teams.Restore | NO | YES | YES | YES | YES | MISSING | NO |
| GET | /api/v2/admin/reference-data/catalogs | NO | YES | YES (uses legacy) | ReferenceData.Read | NO | NO | NO | NO | NO | MISSING | YES |
| GET | /api/v2/admin/reference-data/{catalog} | NO | YES | YES (uses legacy) | ReferenceData.Read | YES | NO | NO | NO | NO | MISSING | YES |
| POST | /api/v2/admin/reference-data/{catalog} | NO | YES | NO | ReferenceData.Update | NO | NO | NO | YES | YES | MISSING | NO |
| PUT | /api/v2/admin/reference-data/{catalog}/{id} | NO | YES | NO | ReferenceData.Update | NO | YES | YES | YES | YES | MISSING | NO |
| GET | /api/v2/admin/reference-data/{catalog}/{id}/deletion-impact | NO | YES | NO | ReferenceData.Read | NO | NO | NO | NO | NO | MISSING | NO |
| DELETE | /api/v2/admin/reference-data/{catalog}/{id} | NO | YES | NO | ReferenceData.Archive | NO | YES | YES | YES | YES | MISSING | NO |
| POST | /api/v2/admin/reference-data/{catalog}/{id}/restore | NO | YES | NO | ReferenceData.Restore | NO | YES | YES | YES | YES | MISSING | NO |
| GET | /api/v2/admin/data-quality/issues | NO | YES | NO | DataQuality.Read | YES | NO | NO | NO | NO | MISSING | NO |
| GET | /api/v2/admin/data-quality/issues/{id} | NO | YES | NO | DataQuality.Read | NO | YES | NO | NO | NO | MISSING | NO |
| POST | /api/v2/admin/data-quality/issues/{id}/assign | NO | YES | NO | DataQuality.Assign | NO | YES | YES | YES | YES | MISSING | NO |
| POST | /api/v2/admin/data-quality/issues/{id}/resolve | NO | YES | NO | DataQuality.Resolve | NO | YES | YES | YES | YES | MISSING | NO |
| POST | /api/v2/admin/data-quality/scan | NO | YES | NO | DataQuality.Scan | NO | NO | NO | YES | YES | MISSING | NO |
| GET | /api/v2/admin/reconciliation/queues | NO | YES | NO | Reconciliation.Read | NO | NO | NO | NO | NO | MISSING | NO |
| GET | /api/v2/admin/reconciliation/cases | NO | YES | NO | Reconciliation.Read | YES | NO | NO | NO | NO | MISSING | NO |
| GET | /api/v2/admin/reconciliation/cases/{id} | NO | YES | NO | Reconciliation.Read | NO | YES | NO | NO | NO | MISSING | NO |
| POST | /api/v2/admin/reconciliation/cases/{id}/resolve | NO | YES | NO | Reconciliation.Resolve | NO | YES | YES | YES | YES | MISSING | NO |
| GET | /api/v2/admin/imports | NO | YES | NO | Imports.Read | YES | NO | NO | NO | NO | MISSING | NO |
| GET | /api/v2/admin/imports/{id} | NO | YES | NO | Imports.Read | NO | NO | NO | NO | NO | MISSING | NO |
| GET | /api/v2/admin/imports/{id}/report | NO | YES | NO | Imports.Read | NO | NO | NO | NO | NO | MISSING | NO |
| GET | /api/v2/admin/users | NO | YES | YES (uses legacy) | Users.Manage | YES | NO | NO | NO | NO | MISSING | YES |
| GET | /api/v2/admin/users/{id} | NO | YES | YES (uses legacy) | Users.Manage | NO | YES | NO | NO | NO | MISSING | YES |
| POST | /api/v2/admin/users | NO | YES | YES (uses legacy) | Users.Manage | NO | NO | NO | YES | YES | MISSING | YES |
| PUT | /api/v2/admin/users/{id} | NO | YES | YES (uses legacy) | Users.Manage | NO | YES | YES | YES | YES | MISSING | YES |
| PUT | /api/v2/admin/users/{id}/roles | NO | YES | NO | Users.Manage | NO | YES | YES | YES | YES | MISSING | NO |
| PUT | /api/v2/admin/users/{id}/status | NO | YES | NO | Users.Manage | NO | YES | YES | YES | YES | MISSING | NO |
| GET | /api/v2/admin/dashboard | NO | YES | NO | Public (admin) | NO | NO | NO | NO | NO | MISSING | NO |
| GET | /api/v2/admin/maintenance/status | NO | YES | NO | Maintenance.Read | NO | NO | NO | NO | NO | MISSING | NO |
| GET | /api/v2/admin/maintenance/tasks | NO | YES | NO | Maintenance.Read | NO | NO | NO | NO | NO | MISSING | NO |
| POST | /api/v2/admin/maintenance/tasks/{taskCode}/execute | NO | YES | NO | Maintenance.Execute | NO | NO | NO | YES | YES | MISSING | NO |

---

## Legacy Routes Still Used by Client

| METHOD | ROUTE | USED_BY | REPLACEMENT |
|--------|-------|---------|-------------|
| GET | /api/Matches | AdminMatchApiClient.GetMatchesAsync | /api/v2/admin/matches |
| GET | /api/MatchEvents?matchId={id} | AdminEventApiClient.GetEventsAsync | /api/v2/admin/matches/{id}/events |
| GET | /api/Players | AdminPlayerApiClient.GetPlayersAsync | /api/v2/admin/players |
| GET | /api/Players/{id} | AdminPlayerApiClient.GetPlayerAsync | /api/v2/admin/players/{id} |
| POST | /api/Players | AdminPlayerApiClient.CreatePlayerAsync | /api/v2/admin/players |
| PUT | /api/Players/{id} | AdminPlayerApiClient.UpdatePlayerAsync | /api/v2/admin/players/{id} |
| GET | /api/Teams | AdminTeamApiClient.GetTeamsAsync | /api/v2/admin/teams |
| GET | /api/Teams/{id} | AdminTeamApiClient.GetTeamAsync | /api/v2/admin/teams/{id} |
| GET | /api/Competitions | AdminReferenceDataApiClient | /api/v2/admin/reference-data/competitions |
| GET | /api/Lookups/* | AdminReferenceDataApiClient | /api/v2/admin/reference-data/{catalog} |
| GET | /api/Users | AdminUsersApiClient | /api/v2/admin/users |
| POST | /api/Users | AdminUsersApiClient | /api/v2/admin/users |
| PUT | /api/Users/{id} | AdminUsersApiClient | /api/v2/admin/users/{id} |

---

## Summary

- LIVE_ADMIN_ROUTES_BEFORE: 10 (v2 admin)
- ADMIN_ROUTES_ADDED_TARGET: 52
- LEGACY_ADMIN_ROUTES_USED_BEFORE: 13
- LEGACY_ADMIN_ROUTES_USED_TARGET_AFTER: 0
- ADMIN_CONTRACT_MISMATCH_COUNT: 0
