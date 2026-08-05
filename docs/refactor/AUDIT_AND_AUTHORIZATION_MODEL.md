# Modèle d'audit et d'autorisation

## Rôles

| Rôle | Portée |
|---|---|
| `VIEWER` | Lecture autorisée uniquement |
| `INTEGRATION_OPERATOR` | Preview et exécution d'imports non destructifs |
| `DATA_EDITOR` | Modification joueuses, matchs et événements |
| `DATA_QUALITY_MANAGER` | Qualité, réconciliation et fusion contrôlée |
| `ADMIN` | Référentiels, équipes et utilisateurs |
| `SUPER_ADMIN` | Suppression contrôlée et maintenance avancée |

## Permissions cibles

| Domaine | Permissions |
|---|---|
| Dashboard | `dashboard.read` |
| Imports | `import.read`, `import.preview`, `import.execute`, `import.rollback` |
| Matchs | `match.read`, `match.write`, `match.delete`, `match.recalculate` |
| Événements | `event.read`, `event.write`, `event.delete` |
| Joueuses | `player.read`, `player.write`, `player.merge` |
| Équipes | `team.manage` |
| Référentiels | `reference.manage` |
| Qualité | `quality.resolve` |
| Audit | `audit.read` |
| Maintenance | `maintenance.execute` |
| Utilisateurs | `users.manage` |

Total : 21 permissions. L'API est l'autorité. Le client masque ou désactive les actions pour l'ergonomie, jamais comme seul contrôle.

## Audit obligatoire

```text
AuditId
TimestampUtc
UserId
UserRole
Action
EntityType
EntityId
Before
After
Reason
CorrelationId
ClientVersion
ApiVersion
Source
Success
ErrorCode
```

L'audit est écrit dans la même transaction que l'opération métier. Pour un dry run, une trace de simulation sans `Before/After` sensible peut être conservée selon la politique de rétention.

## Données interdites dans l'audit

Mot de passe, hash, sel, JWT, secret client, connection string, fichier complet et contenu binaire. Les champs PII sont minimisés et l'accès à l'audit est lui-même contrôlé.

## Correspondance initiale

| Permission | VIEWER | INTEGRATION_OPERATOR | DATA_EDITOR | DATA_QUALITY_MANAGER | ADMIN | SUPER_ADMIN |
|---|---:|---:|---:|---:|---:|---:|
| Lecture métier | Oui | Oui | Oui | Oui | Oui | Oui |
| Preview import | Non | Oui | Oui | Oui | Oui | Oui |
| Execute import | Non | Oui | Oui | Oui | Oui | Oui |
| Écriture métier | Non | Non | Oui | Oui | Oui | Oui |
| Réconciliation/fusion | Non | Non | Non | Oui | Oui | Oui |
| Référentiels/utilisateurs | Non | Non | Non | Non | Oui | Oui |
| Suppression/maintenance | Non | Non | Non | Non | Non | Oui |

La matrice finale doit être gérée côté serveur et testée endpoint par endpoint.
