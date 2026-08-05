# Roadmap d'implémentation

## P0 - Sécuriser et rendre les écritures fiables (12)

1. Révoquer et retirer le secret client du source et des artefacts.
2. Corriger les références Core de la solution API dans une branche API isolée.
3. Créer un projet de tests client et la première CI build/test.
4. Définir DTO et ProblemDetails `/api/v2/admin`.
5. Implémenter les six rôles et 21 permissions côté API.
6. Créer le modèle et le service d'audit transactionnel.
7. Ajouter version/ETag aux entités administrables.
8. Livrer import preview avec hash, mapping et statut doublon.
9. Livrer import execute transactionnel et rapport ligne par ligne.
10. Supprimer tout succès partiel implicite et toute valeur manquante forcée.
11. Livrer deletion-impact, soft delete/archivage et rollback documenté.
12. Couvrir import, doublon, concurrence, audit et suppression par tests.

## P1 - Construire le centre d'administration (15)

1. Découper les dictionnaires de design et composants communs.
2. Refondre shell, navigation et session.
3. Créer le dashboard actionnable.
4. Construire le workflow d'intégration à sept étapes.
5. Créer la liste et le détail Matchs.
6. Créer l'éditeur d'événements adaptatif.
7. Créer l'administration complète des joueuses.
8. Créer équipes, aliases et saisons.
9. Créer le catalogue des référentiels allow-listés.
10. Créer le workspace Qualité.
11. Créer le centre de réconciliation.
12. Créer historique d'import et audit avec diff.
13. Passer toutes les listes à la pagination/recherche serveur.
14. Fermer la matrice d'accessibilité.
15. Mesurer démarrage, navigation, listes, recherche et preview.

## P2 - Enrichir et industrialiser (10)

1. Opérations en masse avec dry run.
2. Maintenance contrôlée et suivi de tâches.
3. Export PDF réel ou retrait du démonstrateur.
4. Cache borné et stratégie hors ligne explicite.
5. Liens contextuels vers analytics HandWStat.
6. Localisation et vocabulaire centralisé.
7. Raccourcis clavier avancés.
8. Colonnes et densité personnalisables.
9. Administration des releases.
10. Tests visuels et budgets de performance continus.

## Phases

| Phase | Contenu | Condition de sortie |
|---|---|---|
| A | Audit et architecture | Documents, baseline, risques identifiés |
| B | Design system et shell | Shell accessible et buildable |
| C | Intégration | Preview/execute transactionnel testé |
| D | Matchs et événements | CRUD sûr, recalcul et suppression |
| E | Joueuses et identités | Identité, transfert, fusion contrôlée |
| F | Équipes et référentiels | Administration allow-listée |
| G | Qualité et réconciliation | Anomalies actionnables |
| H | Audit et sécurité | Permissions et traçabilité complètes |
| I | Performance/accessibilité | Budgets et matrice au vert |
| J | Validation finale | Clone propre, tests, revue manuelle |

## Dépendance critique

Phase B peut préparer le shell, mais Phase C ne doit pas brancher d'écriture réelle avant fermeture des P0 API, audit, secret, transaction et tests.
