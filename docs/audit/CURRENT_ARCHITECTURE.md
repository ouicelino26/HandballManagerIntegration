# Architecture actuelle

## Baseline

| Élément | Valeur |
|---|---|
| Dépôt | `HandballManagerIntegration` |
| Branche source | `master` |
| Commit de départ | `c8bf25df8235d17f4942325f3c84f4807b5c1b01` |
| Branche de travail | `feature/handwstat-admin-platform-v1` |
| Projet local | 1 projet WPF |
| Framework | `.NET 8`, cible `net8.0-windows10.0.19041.0` |
| UI | WPF, ModernWpfUI, XAML et code-behind |
| Architecture | MVVM partiel avec injection de dépendances |
| Tests locaux | Aucun projet de tests |
| Taille inspectée | 8 723 lignes C#/XAML |

Le fichier solution référence aussi `HandballManagerCore` hors du dépôt. Le build Debug réussit avec 133 avertissements. `dotnet test` ne trouve aucun test à exécuter.

## Composants

```text
App / Host .NET
  -> configuration JSON et conteneur DI
  -> LoginWindow
  -> MainWindow et navigation en code-behind
       -> DashboardPage
       -> IntegrationPage
            -> IntegrationViewModel
            -> TimeIntegrationViewModel
       -> PlayersPage
       -> SendPdf
       -> UsersPage
  -> services HTTP
       -> ApiAuthService
       -> ApiService
       -> PlayersApiService
       -> UsersApiService
  -> HandballManagerAPI HTTPS
       -> EF Core / MySQL
```

## Responsabilités actuelles

| Zone | Responsabilité | Observation |
|---|---|---|
| `App.xaml.cs` | Host, DI, configuration, cycle de vie | Composition correcte, mais Service Locator exposé par `App.Services`. |
| `MainWindow` | Shell, session, navigation | Navigation et état de barre latérale entièrement en code-behind. |
| `IntegrationViewModel` | Lecture, mapping, identité, doublons, écritures | 1 204 lignes et plusieurs responsabilités métier/IO/UI. |
| `TimeIntegrationViewModel` | Temps de jeu, résolution match/joueuses, écritures | 820 lignes, logique de rapprochement dupliquée. |
| Services API | Authentification et CRUD | Bonne séparation initiale, erreurs souvent réduites à `bool` ou masquées. |
| Views | Présentation et orchestration | Dashboard, joueuses, comptes et PDF contiennent de la logique métier. |
| Core externe | Entités et DTO partagés | Couplage fort du client aux entités EF et aux détails de sérialisation. |

## Dépendances et accès aux données

- Le client ne contient aucun accès direct MySQL ou SQLite.
- Les écritures durables passent actuellement par l'API.
- Le client consomme toutefois des entités EF (`Match`, `MatchEvent`, `Player`) comme contrats HTTP.
- L'URL configurée est une cible HTTPS distante, non loopback. Aucun appel n'a été effectué pendant cet audit.
- Le jeton utilisateur est gardé uniquement en mémoire et ajouté aux `HttpClient` partagés.
- Les fichiers XLSX sont convertis en CSV à côté du fichier source avant lecture.

## État architectural

### Points solides

- API comme frontière de données.
- DI et `HttpClientFactory` disponibles.
- commandes asynchrones CommunityToolkit utilisées pour les imports.
- sélection de saison et journée déjà portée par les modèles de vue.
- pagination API disponible sur les listes de matchs et de joueuses.

### Limites bloquantes

- aucune transaction englobant match, événements et changements d'équipe ;
- aucune abstraction de résultat homogène, de corrélation ou d'annulation ;
- aucun audit administratif exploité ;
- aucune concurrence optimiste ;
- ViewModels monolithiques et rapprochement d'identité dupliqué ;
- navigation et permissions dispersées dans le code-behind ;
- aucun projet de tests dans le dépôt ;
- configuration sensible versionnée dans la source et dans une archive de release.

## Orientation cible

La migration doit rester progressive : conserver le projet WPF, introduire des services applicatifs par module, des contrats d'API dédiés, un état de navigation centralisé et des composants visuels partagés. Les garanties transactionnelles, d'autorisation, d'audit et de concurrence doivent rester côté API.
