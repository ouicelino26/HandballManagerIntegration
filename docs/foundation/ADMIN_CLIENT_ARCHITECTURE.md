# Administrative client architecture

## Layers

| Layer | Responsibility |
| --- | --- |
| `Admin/Models` | Immutable session, capability, navigation, error, and missing-value contracts. |
| `Admin/Abstractions` | API, session, navigation, dialog, file, clock, dispatcher, and notification boundaries. |
| `Admin/Services` | Administrative HTTP client, JWT handler, capability cache, safe ProblemDetails mapping, and navigation policy. |
| `Admin/Workflows` | Windowless state machines for startup, imports, impact confirmation, concurrency, loading, and error presentation. |
| `Components` and `Themes` | Shared WPF presentation primitives and design tokens without business rules. |
| `Views` | Composition and navigation only. Existing legacy views remain incremental migration targets. |

The new shell does not perform HTTP calls directly. It requests capabilities through `IAdminCapabilitiesService`, then asks `IAdminNavigationService` to build the authorized module list.

## Dependency registration

The generic host registers typed `HttpClient` instances and the `AdminSessionHandler`. The handler is the single administrative path that attaches JWT credentials and centralizes 401 handling. API failures are converted to `AdminClientError`; raw response bodies and stack traces are not exposed.

## Honest delivery states

The shell supports only `FOUNDATION_READY`, `READ_ONLY_AVAILABLE`, `PARTIAL`, `BLOCKED`, and `NOT_IMPLEMENTED`. Existing functional screens are opened for Accueil, Integrations, Joueuses, and Utilisateurs. Other authorized modules render `ModuleStatusPage`, which states their actual delivery status and never invents records or actions.

## Incremental migration boundary

Some pre-existing feature code still contains direct API usage and static dialogs. That legacy debt is not presented as resolved by this P0 foundation. New administrative workflows must use the abstractions above, and future phases should move each legacy module behind dedicated clients and view models.
