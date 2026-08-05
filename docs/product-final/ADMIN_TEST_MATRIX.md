# Admin Test Matrix

Generated: 2026-08-06  
Branch: feature/handwstat-admin-product-final-v1 (Integration) / feature/admin-product-completion-v1 (API)

---

## Integration (WPF) Tests — 63 total, 63 passing

### Pre-existing Tests (22)
Covered core integration file parsing, match import, event parsing, time player reconciliation.

### AdminV2ClientTests.cs (41 new)

#### Route Verification (12 tests)
| Test | Route Verified |
|---|---|
| AdminMatchApiClient_GetMatchesAsync_UsesV2Route | /api/v2/admin/matches |
| AdminEventApiClient_GetEventsAsync_UsesV2Route | /api/v2/admin/matches/{id}/events |
| AdminPlayerApiClient_GetPlayersAsync_UsesV2Route | /api/v2/admin/players |
| AdminPlayerApiClient_GetPlayerAsync_UsesV2Route | /api/v2/admin/players/{id} |
| AdminPlayerApiClient_CreatePlayerAsync_UsesV2Route | POST /api/v2/admin/players |
| AdminPlayerApiClient_UpdatePlayerAsync_UsesV2Route | PUT /api/v2/admin/players/{id} |
| AdminTeamApiClient_GetTeamsAsync_UsesV2Route | /api/v2/admin/teams |
| AdminTeamApiClient_GetTeamAsync_UsesV2Route | /api/v2/admin/teams/{id} |
| AdminUsersApiClient_GetUsersAsync_UsesV2Route | /api/v2/admin/users |
| AdminUsersApiClient_CreateUserAsync_UsesV2Route | POST /api/v2/admin/users |
| AdminReferenceDataApiClient_CompetitionsRoute_IsV2 | /api/v2/admin/reference-data/competitions |
| AdminReferenceDataApiClient_EventsRoute_IsV2 | /api/v2/admin/reference-data/events |

#### Model Structure Tests (10 tests)
| Test | Assertion |
|---|---|
| AdminPageResult_HasItemsPagePageSizeTotal | Shape contract |
| AdminMatchListItemDto_HasRequiredFields | Field presence |
| AdminEventListItemDto_HasRequiredFields | Field presence |
| AdminPlayerListItemDto_FullNameIsFirstPlusLast | Computed property |
| AdminTeamListItemDto_HasIdNameCode | Shape contract |
| AdminUserDto_DoesNotHavePasswordHash | Security: no password field |
| AdminDashboardDto_HasCounters | Counter fields present |
| AdminImportExecutionListItemDto_HasStatusAndDates | Status + dates |
| AdminReferenceItemDto_HasIdNameCode | Shape contract |
| AdminCatalogDto_HasCatalogKeyAndCount | Catalog shape |

#### ViewModel/Navigation Tests (10 tests)
| Test | Assertion |
|---|---|
| AdminShellViewModel_InitializesWithLoadingState | Not null after init |
| AdminShellViewModel_HasDashboardModule | Dashboard in modules |
| AdminShellViewModel_HasMatchesModule | Matches in modules |
| AdminShellViewModel_HasPlayersModule | Players in modules |
| AdminShellViewModel_HasTeamsModule | Teams in modules |
| AdminShellViewModel_HasUsersModule | Users in modules |
| AdminShellViewModel_HasAuditModule | Audit in modules |
| AdminPageViewModelBase_IsBusyDefaultsFalse | IsBusy = false |
| AdminPageViewModelBase_ErrorMessageDefaultsNull | ErrorMessage = null |
| AdminPageViewModelBase_HasCancelToken | CancellationToken exposed |

#### State/Import/Pagination Tests (9 tests)
| Test | Assertion |
|---|---|
| AdminStateView_XamlLoadsWithoutException | XAML valid |
| DashboardView_XamlLoadsWithoutException | XAML valid |
| MatchesView_XamlLoadsWithoutException | XAML valid |
| PlayersAdminView_XamlLoadsWithoutException | XAML valid |
| ImportsViewModel_InitialState_StepIsSource | Step = 0 |
| ImportsViewModel_InitialState_NoErrors | No initial errors |
| ImportsViewModel_HasPreviewCommand | Command not null |
| ImportsViewModel_HasExecuteCommand | Command not null |
| AdminPageRequest_DefaultPage1PageSize50 | Pagination defaults |

---

## API Tests (HandballManagerAPI.Tests)

### AdminProductCompletionTests.cs (22 tests)

| Test | Coverage |
|---|---|
| MatchList_Returns401_WhenNoAuth | Auth enforcement |
| MatchList_Returns403_WhenWrongPermission | Permission enforcement |
| MatchList_ReturnsPaged_WhenAuthorized | Paginated list |
| MatchList_FiltersBy_Search | Search filter |
| MatchList_FiltersBy_CompetitionId | Competition filter |
| MatchList_FiltersBy_Season | Season filter |
| EventList_Returns404_WhenMatchNotFound | 404 on bad matchId |
| EventList_ReturnsEvents_ForKnownMatch | Event list result |
| PlayerList_Returns401_WhenNoAuth | Auth enforcement |
| PlayerList_Returns403_WhenWrongPermission | Permission enforcement |
| PlayerCreate_Returns403_WhenReadOnly | Create permission check |
| TeamList_ReturnsPaged_WhenAuthorized | Team list paged |
| UserList_Returns403_WhenNotAdmin | User list security |
| UserList_NeverReturnsPasswordHash | Security: no password |
| Dashboard_Returns401_WhenNoAuth | Dashboard auth |
| Dashboard_ReturnsCounters_WhenAuthorized | Counter values |
| ImportHistory_ReturnsPaged_WhenAuthorized | Import list |
| ReferenceData_Returns_AllowedCatalogs | Catalog allow-list |
| ReferenceData_Returns404_ForUnknownCatalog | 404 on bad catalog |
| MatchValidate_ReturnsAnalysis_ForKnownMatch | Validate result |
| MatchValidate_Returns404_ForUnknownMatch | Validate 404 |
| MatchValidate_Returns403_WhenNoPermission | Validate auth |

---

## Coverage Summary

| Area | Tests | Status |
|---|---|---|
| Route migration (v2) | 12 | PASS |
| Model/DTO contracts | 10 | PASS |
| ViewModel navigation | 10 | PASS |
| State/XAML/Import | 9 | PASS |
| Pre-existing Integration | 22 | PASS |
| API route + auth | 22 | PASS |
| **Total** | **85** | **ALL PASS** |
