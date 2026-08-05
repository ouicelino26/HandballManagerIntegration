# Administrative navigation capability model

## Source of truth

After login, the client calls `GET /api/v2/admin/capabilities`. Only capabilities returned with `allowed=true` are retained. A local role name alone never grants or reveals a module.

If the capability request fails, expires, or is rejected, the service clears its capability cache and the shell closes navigation. This fail-closed behavior prevents stale permissions from keeping a module accessible.

## Module mapping

| Module | Required capability | Current status |
| --- | --- | --- |
| Accueil | `AdminDashboard.Read` | `FOUNDATION_READY` |
| Integrations | `Imports.Read` | `PARTIAL` |
| Matchs | `Matches.Read` | `READ_ONLY_AVAILABLE` |
| Joueuses | `Players.Read` | `PARTIAL` |
| Equipes | `Teams.Read` | `NOT_IMPLEMENTED` |
| Evenements | `Events.Read` | `READ_ONLY_AVAILABLE` |
| Referentiels | `ReferenceData.Manage` | `NOT_IMPLEMENTED` |
| Qualite des donnees | `DataQuality.Manage` | `NOT_IMPLEMENTED` |
| Historique et audit | `Audit.Read` | `READ_ONLY_AVAILABLE` |
| Maintenance | `AdminDashboard.Read` | `NOT_IMPLEMENTED` |
| Utilisateurs et droits | `Users.Manage` | `PARTIAL` |
| Parametres | `AdminDashboard.Read` | `FOUNDATION_READY` |

Visibility is a usability measure, not an authorization boundary. The API must continue to enforce its policy on every read or write.

## Keyboard and accessibility foundation

The navigation is a standard WPF `ListBox`: Tab enters the list, arrow keys change selection, and the selected item has a distinct state. The list and shell actions expose automation names, keyboard focus has a visible border, and the sidebar remains scrollable when the window is short. The collapse action changes its automation label between "Replier" and "Deplier".
