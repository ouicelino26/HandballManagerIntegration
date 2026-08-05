# Stratégie de tests

## Baseline Phase A

| Vérification | Résultat |
|---|---|
| Build client Debug | PASS, 0 erreur, 133 avertissements |
| Tests client | Aucun test découvert |
| Build solution API | BLOCKED, référence Core invalide |
| Base de production | Non utilisée |
| API distante | Aucun appel pendant l'audit |

## Pyramide cible

| Niveau | Portée | Outils proposés |
|---|---|---|
| Unitaires | normalisation, validation, diff, hash, permissions | xUnit |
| Application | ViewModels et cas d'usage avec fakes | xUnit + CommunityToolkit |
| Contrats HTTP | sérialisation, ProblemDetails, ETag | WireMock.Net ou TestServer |
| API intégration | transaction, rollback, audit, concurrence | WebApplicationFactory + conteneur MySQL de test |
| UI | navigation, focus, formulaires, états | FlaUI/UI Automation |
| Visuels | shell, dialogues, densités, DPI | captures contrôlées + revue |

## Suites prioritaires

- navigation et permissions ;
- login, expiration et logout ;
- mapping CSV/XLSX et dates/temps ;
- identité joueuse sans fusion approximative ;
- hash, idempotence et doublons ;
- preview sans écriture ;
- execute transactionnel ;
- import partiel et rollback ;
- modification match/événement avec recalcul ;
- impact et suppression ;
- concurrence 409/412 ;
- audit sans secret ;
- pagination, recherche, annulation et erreurs.

## Données de test

Utiliser des fixtures synthétiques minimales, clairement nommées, et des copies anonymisées autorisées. Aucun test n'utilise la production, une connection string de production ou les fichiers réels sans accord explicite.

## Clone propre

Après chaque phase poussée : créer un clone temporaire, restaurer, construire, tester, lancer le scan de secrets et vérifier qu'aucun fichier local non suivi n'est requis. Le clone est supprimé seulement après conservation du rapport de validation.

## Gates

- build sans erreur ;
- nouveaux avertissements interdits ;
- tests P0 au vert ;
- secret scan au vert ;
- aucune écriture externe pendant tests ;
- accessibilité critique au vert ;
- rapport de migration/API joint quand un dépôt connexe change.
