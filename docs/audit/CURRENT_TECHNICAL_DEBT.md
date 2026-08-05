# Dette technique actuelle

| ID | Priorité | Dette | Effet | Taille | Dépendance |
|---|---|---|---|---|---|
| TD-001 | P0 | Secret et archive sensibles suivis par Git | Risque sécurité immédiat | S | Rotation manuelle |
| TD-002 | P0 | Import match non transactionnel | Matchs partiels et équipes modifiées à tort | L | API admin imports |
| TD-003 | P0 | Échec événement ignoré puis succès possible | Faux positif opérateur | M | API import transactionnel |
| TD-004 | P0 | Suppressions physiques directes | Perte irréversible | L | API impact/audit |
| TD-005 | P0 | Aucun test dans le client | Régressions non détectées | M | Projet de tests |
| TD-006 | P0 | Solution API avec références Core invalides | Baseline API impossible | S | Branche API séparée |
| TD-007 | P1 | `IntegrationViewModel` de 1 204 lignes | Couplage et faible testabilité | L | Services applicatifs |
| TD-008 | P1 | `TimeIntegrationViewModel` de 820 lignes | Logique dupliquée | L | Service identité/import |
| TD-009 | P1 | Service Locator `App.Services` | Dépendances cachées | M | Navigation/DI |
| TD-010 | P1 | Logique métier en code-behind | Tests et maintenance difficiles | L | MVVM progressif |
| TD-011 | P1 | Entités EF utilisées comme contrats client | Sérialisation fragile | M | DTO admin versionnés |
| TD-012 | P1 | Erreurs réduites à `bool` ou avalées | Diagnostic impossible | M | `OperationResult` |
| TD-013 | P1 | `catch` vide dans le chargement des fichiers | Erreur silencieuse | S | Gestion d'erreur commune |
| TD-014 | P1 | Événement inconnu remplacé par ID 37 | Corruption sémantique | S | Validation bloquante |
| TD-015 | P1 | Données absentes remplacées par zéro | Statistiques fausses | S | Modèles nullable/validation |
| TD-016 | P1 | Rapprochement de joueuses heuristique dupliqué | Mauvaise identité possible | L | Réconciliation serveur |
| TD-017 | P1 | Prénom/nom déduits de `FullName` | Noms composés corrompus | M | DTO joueur détaillé |
| TD-018 | P1 | Listes chargées intégralement | Latence et mémoire | M | Pagination serveur UI |
| TD-019 | P1 | Logs texte dans le répertoire courant | PII, concurrence et rétention | M | Logging structuré |
| TD-020 | P1 | CSV écrit à côté du XLSX | Mutation du dossier source | S | Lecture XLSX directe/temp sécurisé |
| TD-021 | P2 | Dictionnaire de styles monolithique | Réutilisation limitée | M | Design system découpé |
| TD-022 | P2 | Styles locaux répétés | Incohérences visuelles | M | Composants partagés |
| TD-023 | P2 | Navigation codée en dur | Modules/permissions difficiles | M | Registre de navigation |
| TD-024 | P2 | Studio PDF fictif | Confusion produit | S | Retrait ou implémentation réelle |
| TD-025 | P2 | 133 avertissements de build | Bruit masquant les défauts | M | Nullable + SDK moderne |

## Ordre de traitement

La dette P0 doit être fermée avant la refonte visuelle. La première tranche d'implémentation doit sécuriser les secrets, créer le socle de tests et définir les endpoints transactionnels. Les refactors de ViewModels doivent ensuite suivre les frontières de cas d'usage, sans déplacement massif du projet.
