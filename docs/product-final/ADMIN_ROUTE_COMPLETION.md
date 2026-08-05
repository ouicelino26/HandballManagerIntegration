# Admin Route Completion Report

Generated: 2026-08-06  
Branch (Integration): feature/handwstat-admin-product-final-v1  
Branch (API): feature/admin-product-completion-v1

---

## Legacy Route Migration (WPF Client)

All 13 legacy routes replaced with v2 admin routes.

| Legacy Route | V2 Route | Status |
|---|---|---|
| GET /api/Matches | GET /api/v2/admin/matches | MIGRATED |
| GET /api/MatchEvents?matchId={id} | GET /api/v2/admin/matches/{id}/events | MIGRATED |
| GET /api/Players | GET /api/v2/admin/players | MIGRATED |
| GET /api/Players/{id} | GET /api/v2/admin/players/{id} | MIGRATED |
| POST /api/Players | POST /api/v2/admin/players | MIGRATED |
| PUT /api/Players/{id} | PUT /api/v2/admin/players/{id} | MIGRATED |
| GET /api/Teams | GET /api/v2/admin/teams | MIGRATED |
| GET /api/Teams/{id} | GET /api/v2/admin/teams/{id} | MIGRATED |
| GET /api/Competitions | GET /api/v2/admin/reference-data/competitions | MIGRATED |
| GET /api/Lookups/* | GET /api/v2/admin/reference-data/{catalog} | MIGRATED |
| GET /api/Users | GET /api/v2/admin/users | MIGRATED |
| POST /api/Users | POST /api/v2/admin/users | MIGRATED |
| PUT /api/Users/{id} | PUT /api/v2/admin/users/{id} | MIGRATED |

**LEGACY_ROUTES_REMAINING: 0**

---

## New API Controllers Added

| Controller | Routes | Auth | Tests |
|---|---|---|---|
| AdminMatchListController | GET /api/v2/admin/matches | Matches.Read | YES |
| AdminEventListController | GET /api/v2/admin/matches/{id}/events | Matches.Read | YES |
| AdminPlayersController | GET/POST /api/v2/admin/players, GET/PUT/DELETE/POST /{id}/* | Players.* | YES |
| AdminTeamsController | GET/POST /api/v2/admin/teams, GET/PUT/DELETE/POST /{id}/* | Teams.* | YES |
| AdminUsersController | GET/POST /api/v2/admin/users, GET/PUT /{id}, PUT /{id}/roles, PUT /{id}/status | Users.Manage | YES |
| AdminDashboardController | GET /api/v2/admin/dashboard | Admin (authenticated) | YES |
| AdminImportHistoryController | GET /api/v2/admin/imports, GET /{id} | Imports.Read | YES |
| AdminReferenceDataController | GET catalogs, GET {catalog}, PUT {catalog}/{id} | ReferenceData.* | YES |
| AdminMatchesController (validate) | POST /api/v2/admin/matches/{id}/validate | Matches.Read | YES |

---

## Contract Guarantees

- All paginated list endpoints return `AdminPageResult<T>` with `Items`, `Page`, `PageSize`, `TotalCount`
- All single-entity GET endpoints return ETag header
- All write endpoints require `If-Match` header (optimistic concurrency)
- All writes wrapped in DB transaction + AdminAuditService call
- Soft-delete pattern: DELETE archives (IsDeleted=true), POST /restore reverses
- Deletion impact check available before archive on all entity types
- No password hash ever returned from user endpoints
- ProblemDetails (RFC 7807) with ADMIN_* codes on all errors
