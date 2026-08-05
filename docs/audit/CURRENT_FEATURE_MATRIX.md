# Matrice fonctionnelle actuelle

Statuts : `COMPLETE`, `PARTIAL`, `BROKEN`, `LEGACY`, `MISSING`, `BLOCKED_BY_API`, `BLOCKED_BY_DATABASE`.

| FEATURE | SCREEN | VIEWMODEL | SERVICE | API_ENDPOINT | DATABASE_TABLES | AUTHORIZATION | VALIDATION | AUDIT | TESTS | STATUS | RISK |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Connexion | LoginWindow | Code-behind | ApiAuthService | `POST /auth/login` | users | Admin client + API | Champs requis | Non | Aucun | PARTIAL | Erreurs internes affichées, pas de refresh. |
| Autorisation UI | MainWindow | MainViewModel | ApiService | `GET /api/Users/me` | users | Admin uniquement | Contrôle au chargement | Non | Aucun | PARTIAL | Modèle à 2 rôles seulement. |
| Tableau de bord | DashboardPage | Code-behind | ApiService, PlayersApiService | Users/me, Players | users, players, teams | Admin | Faible | Non | Aucun | PARTIAL | KPI non reliés à des actions et chargement complet. |
| Intégration match | IntegrationPage | IntegrationViewModel | Import CSV/XLSX, PlayersApiService | Matches, MatchEvents, Players, Teams, référentiels | matchs, matchevents, players | Admin | Partielle | Logs locaux | Aucun | BROKEN | Contrat équipe fragile et écritures partielles observées. |
| Intégration temps de jeu | IntegrationPage | TimeIntegrationViewModel | TimePlayersSheetImportService | TimePlayers, Matches, Players, Teams | timeplayers, matchs, players | Admin | Partielle | Logs locaux | Aucun | PARTIAL | POST par ligne, résultat partiel possible. |
| Création joueuse pendant import | AddPlayerWindows | Code-behind | Appel HTTP direct | `POST /api/Players` | players | Admin | Champs requis | Non | Aucun | PARTIAL | Valeur absente transformable en zéro. |
| Import CSV | IntegrationPage | IntegrationViewModel | MatchFileImportService | Indirect | Aucune directe | Admin | En-têtes CsvHelper | Non | Aucun | PARTIAL | CSV temporaire non échappé et laissé sur disque. |
| Import Excel | IntegrationPage | Deux ViewModels | ClosedXML | Indirect | Aucune directe | Admin | Nom de fichier/feuille | Non | Aucun | PARTIAL | Détection fondée sur conventions de chemin. |
| Import PDF | Aucun | Aucun | Aucun | Aucun | Aucun | Aucun | Aucune | Non | Aucun | MISSING | Fonction demandée mais inexistante. |
| Validation fichier | IntegrationPage | Deux ViewModels | Services d'import | Plusieurs | Plusieurs | Admin | Incomplète | Logs locaux | Aucun | PARTIAL | Pas de rapport structuré ni sévérités. |
| Prévisualisation import | Aucun | Aucun | Aucun | Aucun | Aucun | Aucun | Aucune | Non | Aucun | MISSING | Écriture sans preview métier. |
| Comparaison avant/après | Aucun | Aucun | Aucun | Aucun | Aucun | Aucun | Aucune | Non | Aucun | MISSING | Impact invisible avant validation. |
| Gestion des erreurs | Plusieurs | Dispersé | Dispersé | Réponses brutes | N/A | Admin | Variable | Logs locaux | Aucun | PARTIAL | Exceptions/PII possibles dans UI et logs. |
| Historique imports | Aucun | Aucun | Aucun | Aucun | Aucun registre d'import | Aucun | Aucune | Non | Aucun | MISSING | Pas de hash, statut ou rapport persistant. |
| Gestion utilisateurs | UsersPage | Code-behind | UsersApiService | Users GET/POST | users | Admin | Basique | Non | Aucun | PARTIAL | Pas d'édition, droits fins, audit ou concurrence. |
| Gestion releases | Aucun | Aucun | Aucun | Releases existant côté API | tables app_release | Aucun écran | Aucune | Partiel API | Aucun client | MISSING | Archive binaire suivie sans workflow UI. |
| Gestion joueuses | PlayersPage | Code-behind | PlayersApiService | Players GET/PUT/DELETE | players | Admin | Basique | Non | Aucun | PARTIAL | Nom complet découpé de façon ambiguë, suppression directe. |
| Gestion équipes | Aucun | Aucun | Lecture via PlayersApiService | Teams GET | teams | Lecture Admin | Aucune | Non | Aucun | MISSING | Pas de CRUD ou aliases. |
| Gestion matchs | Aucun | Aucun | Aucun | Matches CRUD legacy | matchs | API Admin | API faible | Non | Aucun | MISSING | Pas d'écran ni impact/concurrence. |
| Gestion événements | Aucun | Aucun | Aucun | MatchEvents CRUD legacy | matchevents | API Admin | API faible | Non | Aucun | MISSING | Pas d'éditeur adapté au type. |
| Référentiels | Formulaires joueuse/import | Aucun dédié | PlayersApiService + appels directs | Lookups, Event, Attacks, Defenses | référentiels | Lecture Admin | Faible | Non | Aucun | PARTIAL | Lecture éparse, pas d'administration allow-listée. |
| Suppression contrôlée | PlayersPage | Code-behind | PlayersApiService | `DELETE /api/Players/{id}` | players et dépendances | Admin | Confirmation oui/non | Non | Aucun | BROKEN | Aucun motif, impact, soft delete ou rollback. |
| Audit administratif | Aucun | Aucun | Aucun | Aucun | HistoMatch/HistoPlayer inutilisées | Aucun | Aucune | Non | Aucun | MISSING | Traçabilité réglementaire absente. |
| Qualité des données | Aucun | Aucun | Aucun | Aucun | Plusieurs | Aucun | Aucune | Non | Aucun | MISSING | Anomalies non calculées ni assignables. |
| Maintenance | Aucun | Aucun | Aucun | Système/release partiel | Plusieurs | Aucun écran | Aucune | Non | Aucun | MISSING | Pas de tâches contrôlées. |
| Réconciliation identité | FoundPlayersWindows | IntegrationViewModel dupliqué | Recherche joueuses | Players search/byfullname | players, matchevents | Admin | Score local | Log texte | Aucun | PARTIAL | Auto-sélection sans identité forte. |
| Studio PDF | SendPdf | Code-behind | Aucun | Aucun | Aucun | Admin | Aucune | Non | Aucun | LEGACY | Démonstrateur avec données fictives, aucun export. |
| Santé API/version | Shell/Dashboard | MainViewModel/code-behind | ApiService | Users/me | users | Admin | Booléen | Non | Aucun | PARTIAL | Version client/API non affichée. |

## Totaux

| Mesure | Valeur |
|---|---:|
| Fonctionnalités inventoriées | 28 |
| Complete | 0 |
| Partial | 14 |
| Broken | 2 |
| Legacy | 1 |
| Missing | 11 |

Aucune fonctionnalité ne satisfait aujourd'hui l'ensemble des critères CRUD, audit, autorisation, concurrence et tests imposés par la mission.
