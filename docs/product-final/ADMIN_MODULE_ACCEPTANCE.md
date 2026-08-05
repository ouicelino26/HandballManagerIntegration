# Admin Module Acceptance Checklist

Generated: 2026-08-06  
Branch: feature/handwstat-admin-product-final-v1 (Integration) / feature/admin-product-completion-v1 (API)

---

## Module: Matches

- [x] List with server-side pagination
- [x] Filter by search, competitionId, season, day, teamId, date range, state
- [x] Sort by date, teams, event count
- [x] ETag on single-match GET
- [x] Validate endpoint (event count, score analysis, warnings)
- [x] Archive (soft-delete) with reason
- [x] Restore archived match
- [x] Deletion impact check

## Module: Match Events

- [x] Paginated event list per match
- [x] Filter by teamId, playerId, eventTypeId, period
- [x] ETag on single event GET
- [x] Archive + restore per event
- [x] Deletion impact check per event

## Module: Players

- [x] Paginated player list
- [x] Filter by teamId, search, position, nationality, active state
- [x] Single player GET with ETag
- [x] Create player with transaction + audit
- [x] Update player with If-Match concurrency
- [x] Deletion impact check (how many matches affected)
- [x] Archive (soft-delete)
- [x] Restore

## Module: Teams

- [x] Paginated team list with player count + match count
- [x] Single team GET with ETag
- [x] Create team (transaction + audit)
- [x] Update team with If-Match
- [x] Deletion impact check
- [x] Archive + restore

## Module: Users

- [x] Paginated user list (no password hash)
- [x] Single user GET (no password hash)
- [x] Create user (transaction + audit)
- [x] Update user profile with If-Match
- [x] Update roles (transaction + audit)
- [x] Update status (activate/deactivate)
- [x] Per-user audit trail

## Module: Reference Data

- [x] Catalog allow-list (competitions, positions, events, attacks, defenses, nationalities)
- [x] Paginated items per catalog
- [x] Update catalog item with If-Match + audit + transaction
- [x] 404 on unknown catalog key

## Module: Import

- [x] Preview with dry-run validation
- [x] Execute with idempotence token
- [x] Import execution history (paginated)
- [x] Single execution detail

## Module: Dashboard

- [x] Counters: matches, events, players, teams, users, active sessions
- [x] Last 5 import executions
- [x] Requires authenticated admin

## Module: Audit

- [x] Paginated audit trail (read-only)
- [x] Filter by entity type, user, date range

---

## Cross-Cutting

- [x] All writes: transaction + AdminAuditService
- [x] All writes: require If-Match (optimistic concurrency)
- [x] All GETs (single): return ETag header
- [x] Soft-delete on all entity types (IsDeleted + reason + timestamp + actor)
- [x] ProblemDetails (RFC 7807) with ADMIN_* codes
- [x] JWT Bearer auth on all admin endpoints
- [x] AdminPermissions policy enforcement per route
- [x] No password hash ever returned
- [x] 63 tests passing (Integration) + 22 tests (API)
