# Exigences API Admin

## Version et conventions

- Préfixe : `/api/v2/admin`.
- Contrats dédiés, sans exposition directe des entités EF.
- `ProblemDetails` avec `code`, `correlationId`, `entityType`, `entityId` et erreurs de validation.
- Pagination avec `items`, `page`, `pageSize`, `totalItems`, `totalPages`.
- Tri et filtres allow-listés.
- `CancellationToken` sur toutes les opérations async.
- ETag ou `Version` obligatoire pour les écritures concurrentes.

## Contrat minimum Phase C-H

```text
GET    /api/v2/admin/dashboard

GET    /api/v2/admin/matches
GET    /api/v2/admin/matches/{id}
PATCH  /api/v2/admin/matches/{id}
GET    /api/v2/admin/matches/{id}/deletion-impact
DELETE /api/v2/admin/matches/{id}
POST   /api/v2/admin/matches/{id}/recalculate
POST   /api/v2/admin/matches/{id}/validate

GET    /api/v2/admin/matches/{id}/events
POST   /api/v2/admin/matches/{id}/events
PATCH  /api/v2/admin/matches/{id}/events/{eventId}
DELETE /api/v2/admin/matches/{id}/events/{eventId}

GET    /api/v2/admin/players
GET    /api/v2/admin/players/{id}
POST   /api/v2/admin/players
PATCH  /api/v2/admin/players/{id}

GET    /api/v2/admin/player-identity/conflicts
POST   /api/v2/admin/player-identity/resolve

GET    /api/v2/admin/data-quality/issues
POST   /api/v2/admin/data-quality/issues/{id}/resolve

GET    /api/v2/admin/imports
POST   /api/v2/admin/imports/preview
POST   /api/v2/admin/imports/execute

GET    /api/v2/admin/audit
```

Total minimum : 24 endpoints.

## Import preview

La requête contient fichier, compétition, saison, journée, date choisie, type et version client. La réponse contient `PreviewId`, SHA-256, expiration, statut doublon, mapping, erreurs, avertissements, diff, opérations proposées et version de mapping. Aucune écriture métier n'est autorisée pendant preview.

## Import execute

La requête contient `PreviewId`, décisions explicites, `Reason`, `DryRun`, `ExpectedVersions` et `CorrelationId`. L'API revalide le hash et l'expiration, ouvre une transaction, applique ou simule, écrit l'audit, recalcule puis retourne un rapport par ligne.

## Réponses de concurrence

- `409 Conflict` pour conflit métier ou import concurrent.
- `412 Precondition Failed` pour ETag/version obsolète.
- Réponse avec version client, version serveur et champs divergents.

## Évolution requise hors dépôt

La mise en œuvre exige une branche séparée dans API et probablement Core pour DTO, audit, versions et statuts. Aucun de ces dépôts n'a été modifié pendant la Phase A.
