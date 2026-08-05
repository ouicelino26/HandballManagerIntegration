# Périmètre fonctionnel cible

## Modules de navigation

| # | Module | Phase | Résultat attendu |
|---:|---|---|---|
| 1 | Accueil | B | Anomalies et tâches actionnables |
| 2 | Intégrations | C | Workflow preview/execute/rapport |
| 3 | Matchs | D | Liste, détail, correction contrôlée |
| 4 | Joueuses | E | CRUD sûr, transfert et identité |
| 5 | Équipes | F | CRUD, aliases et saisons |
| 6 | Événements | D | Éditeur adapté au type |
| 7 | Référentiels | F | Catalogue allow-listé |
| 8 | Qualité des données | G | Détection, assignation, résolution |
| 9 | Historique et audit | H | Recherche et diff avant/après |
| 10 | Maintenance | H/I | Tâches autorisées et suivies |
| 11 | Utilisateurs et droits | H | Six rôles et permissions |
| 12 | Paramètres | B/H | API, apparence, diagnostic filtré |

## Scope MVP administrable

- authentification et permissions ;
- import match/événements avec preview, idempotence et transaction ;
- import temps de jeu ;
- listes et détails matchs, événements, joueuses ;
- correction avec audit et concurrence ;
- suppression/archivage avec impact ;
- qualité et réconciliation d'identité ;
- historique des imports et audit ;
- référentiels prioritaires ;
- utilisateurs et droits.

## Scope ultérieur

- opérations en masse avancées ;
- studio PDF réel ;
- personnalisation des colonnes ;
- maintenance planifiée ;
- analytics avancés intégrés au contexte administratif ;
- thème sombre après validation d'accessibilité.

## Critères de complétude

Une fonctionnalité n'est complète que si son chemin lecture/écriture, validation, permission, audit, concurrence, erreur, chargement, état vide et tests est livré. Pour une suppression s'ajoutent impact, motif, transaction, recalcul et rollback.
