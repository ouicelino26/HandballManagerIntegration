# Analyse des écarts API d'administration

## Inventaire

| Mesure | Valeur |
|---|---:|
| Routes HTTP actuelles | 99 |
| Routes sous `/api/admin` | 6, toutes dédiées aux releases |
| Routes legacy protégées Admin | 29 |
| Contrats minimum `/api/v2/admin` requis | 24 |
| Contrats minimum présents à l'identique | 0 |
| Contrats minimum manquants | 24 |

Les routes CRUD legacy constituent des briques techniques, mais ne satisfont pas les garanties d'impact, audit, concurrence, dry run et transaction. Elles ne sont donc pas comptées comme endpoints administratifs complets.

## Contrat minimum proposé

| Groupe | Nombre | Routes attendues |
|---|---:|---|
| Dashboard | 1 | `GET /api/v2/admin/dashboard` |
| Matchs | 7 | liste, détail, patch, deletion-impact, delete, recalculate, validate |
| Événements | 4 | liste, create, patch, delete sous un match |
| Joueuses | 4 | liste, détail, create, patch |
| Identité | 2 | conflits, résolution |
| Qualité | 2 | anomalies, résolution |
| Imports | 3 | historique, preview, execute |
| Audit | 1 | recherche audit |

## Matrice des besoins

| NEED | EXISTING_ENDPOINT | MISSING_ENDPOINT | REQUIRED_PERMISSION | VALIDATION_RULES | TRANSACTION_SCOPE | AUDIT_REQUIREMENT | DELETION_IMPACT | API_CHANGE_REQUIRED |
|---|---|---|---|---|---|---|---|---|
| Dashboard actionnable | Stats/Players dispersés | `/api/v2/admin/dashboard` | `dashboard.read` | Scope autorisé | Lecture cohérente | Non | N/A | YES |
| Liste matchs admin | `GET /api/Matches` | `/api/v2/admin/matches` | `match.read` | Filtres/tri/page allow-listés | Lecture | Non | N/A | YES |
| Détail match admin | `GET /api/Matches/{id}` + endpoints séparés | `/api/v2/admin/matches/{id}` | `match.read` | Entité existante | Snapshot lecture | Non | Inclus résumé | YES |
| Modifier match | `PUT /api/Matches/{id}` | `PATCH /api/v2/admin/matches/{id}` | `match.write` | Équipes, saison, score, version | Match + recalculs | Before/after + motif | Preview conséquences | YES |
| Supprimer match | DELETE legacy | impact + DELETE admin | `match.delete` | Motif, confirmation, version | Toutes dépendances | Obligatoire | Obligatoire | YES |
| Recalculer match | Summary lecture seule | `/recalculate` | `match.recalculate` | Mode et scope | Agrégats match/saison | Obligatoire | N/A | YES |
| Valider match | Aucun | `/validate` | `quality.resolve` | Catalogue de règles | Lecture/snapshot | Résultat auditable | N/A | YES |
| Lister événements | `GET /api/MatchEvents` | route sous match | `event.read` | Page/tri/filtres | Lecture | Non | N/A | YES |
| Créer événement | POST legacy | route sous match | `event.write` | Champs par type | Événement + score/agrégats | Obligatoire | Conséquences | YES |
| Modifier événement | PUT legacy | PATCH admin | `event.write` | Version + config type | Événement + recalculs | Obligatoire | Conséquences | YES |
| Supprimer événement | DELETE legacy | DELETE admin | `event.delete` | Motif + version | Événement + recalculs | Obligatoire | Obligatoire | YES |
| Administrer joueuses | CRUD legacy | 4 routes admin | `player.read/write` | Identité, FK, doublons | Joueuse + historique | Obligatoire en écriture | Pour archive/fusion | YES |
| Résoudre identité | Recherche approximative | 2 routes identity | `player.merge` | Identifiants forts | Joueuses + événements + temps | Obligatoire | Preview fusion | YES |
| Qualité des données | Aucun | 2 routes quality | `quality.resolve` | RuleCode/status/version | Selon résolution | Obligatoire | Selon action | YES |
| Prévisualiser import | Aucun | `/imports/preview` | `import.preview` | Fichier, hash, contexte | Aucune écriture | Trace de simulation | Inclus | YES |
| Exécuter import | CRUD ligne par ligne | `/imports/execute` | `import.execute` | Preview valide/version | Match + événements + joueuses | Obligatoire | Rapport/rollback | YES |
| Historique imports | Aucun | `GET /imports` | `import.read` | Page/filtre/rétention | Lecture | Non | N/A | YES |
| Consulter audit | HistoMatch/HistoPlayer non utilisés | `GET /audit` | `audit.read` | Filtres allow-listés | Lecture | N/A | N/A | YES |

## Autres écarts à planifier

- CRUD équipes, aliases et saisons ;
- référentiels allow-listés ;
- gestion utilisateurs avec les six rôles ;
- restauration/rollback d'import ;
- opérations en masse avec dry run ;
- maintenance et état des tâches ;
- ETag/version sur toutes les entités administrables.

## Contraintes constatées

- Aucun modèle d'audit complet dans Core.
- `HistoMatch` et `HistoPlayer` existent mais ne sont écrits par aucun contrôleur.
- Aucun champ row version/ETag/soft delete sur match, événement ou joueuse.
- Les contrôleurs utilisent directement `HBdbcontext` pour les écritures.
- La solution API et plusieurs projets référencent Core avec un chemin local invalide ; correction à isoler dans une branche API.

`API_PROJECT_MODIFIED=NO` et `CORE_PROJECT_MODIFIED=NO` pendant cette phase.
