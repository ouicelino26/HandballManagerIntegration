# Architecture cible

## Vue logique

```text
Presentation WPF
  Shell, Views, composants, états et ViewModels
          |
Application
  Cas d'usage, validation client, permissions d'affichage
          |
Infrastructure
  API clients typés, fichiers, cache borné, logging
          |
HandballManagerAPI /api/v2/admin
  Autorisation, services métier, transactions, audit
          |
EF Core / repositories
          |
MySQL hbdb
```

## Découpage progressif du client

```text
Core/
  Abstractions, Results, Validation, Security
Application/
  Imports, Matches, Events, Players, Teams,
  ReferenceData, DataQuality, Audit, Maintenance
Infrastructure/
  Api, Files, Logging, Cache, Configuration
Presentation/
  Shell, Views, ViewModels, Components,
  Behaviors, Converters, Themes
```

Le déplacement physique n'est pas un objectif en soi. Chaque nouveau cas d'usage adopte cette direction, puis le code existant est extrait lorsqu'il est touché.

## Règles de dépendance

- Une View ne connaît aucun endpoint.
- Un ViewModel ne sérialise aucune entité EF.
- Un client API typé retourne `OperationResult<T>` avec code, message et correlationId.
- Les modèles de présentation sont indépendants des DTO réseau.
- Les validations ergonomiques peuvent être dupliquées côté client, mais l'API tranche.
- Les permissions UI proviennent de la session, l'API les revérifie.
- Aucun service métier n'est obtenu via `App.Services` hors composition/navigation transitoire.

## Services transverses

| Service | Responsabilité |
|---|---|
| `ISessionService` | Identité, rôles, permissions, expiration |
| `IAuthorizedApiClient` | Auth, ProblemDetails, correlationId, retry sûr |
| `INavigationService` | Registre de modules et historique |
| `IOperationRunner` | Occupation, annulation, progression réelle |
| `IFileInspectionService` | Métadonnées, hash SHA-256, format |
| `IImportAdminClient` | Preview, execute, rapport, historique |
| `IPlayerIdentityClient` | Recherche forte et réconciliation |
| `IErrorPresentationService` | Message utilisateur et diagnostic filtré |
| `IAuditClient` | Recherche et diff avant/après |

## États communs

Tous les écrans de données exposent `Idle`, `Loading`, `Loaded`, `Empty`, `Error`, `PermissionDenied`. Les opérations longues ajoutent `Pending`, `Running`, `Completed`, `Partial`, `Failed`, `Cancelled`, `RolledBack`.

## Frontières transactionnelles

La transaction est côté API pour : import match, import temps, correction majeure, événement avec recalcul, suppression, fusion d'identité, bulk action et référentiel lié. Le client ne tente jamais de simuler une transaction par une suite de requêtes.

## Concurrence

Les DTO administrables portent `Version` ou `ETag`, `UpdatedAt`, `UpdatedBy`. Les commandes envoient `If-Match`/`ExpectedVersion`. Un 409/412 ouvre un comparateur et n'écrase jamais la version serveur.
