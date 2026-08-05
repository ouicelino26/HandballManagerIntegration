# Cycle de vie match et événements

## Cycle d'import

```text
SOURCE_SELECTED
  -> INSPECTED
  -> MAPPED
  -> VALIDATED
  -> PREVIEWED
  -> DECISION_REQUIRED
  -> RUNNING
  -> COMPLETED | PARTIAL | FAILED | CANCELLED | ROLLED_BACK
```

`PARTIAL` n'est jamais présenté comme un succès. Une opération partielle doit préciser les écritures validées, rejetées et la stratégie de reprise.

## Statuts d'idempotence

| Statut | Signification | Action autorisée |
|---|---|---|
| `NEW` | Aucun match proche | Créer après confirmation |
| `EXACT_DUPLICATE` | Hash/signature et contenu identiques | Refuser par défaut |
| `POSSIBLE_DUPLICATE` | Identité métier proche | Comparer et décider |
| `UPDATE_EXISTING` | Diff compatible sur match identifié | Prévisualiser patch |
| `CONFLICT` | Données incompatibles | Résolution obligatoire |
| `REQUIRES_REVIEW` | Identité/référence incertaine | File de réconciliation |

## Signature métier

La détection combine hash de fichier, identifiant externe, compétition, saison, journée, date, équipes ordonnées, score et multiensemble des empreintes d'événements. Une différence de score ou d'ordre d'équipes ne déclenche jamais un écrasement automatique.

## Modification d'un match

1. Charger détail, version, événements et anomalies.
2. Modifier un DTO dédié.
3. Valider équipe, saison, journée, score, date et dépendances.
4. Afficher diff et recalculs.
5. Envoyer motif et version attendue.
6. Appliquer transaction, audit et recalcul côté API.
7. Retourner nouvelle version et rapport.

## Configuration d'événement

Chaque type d'événement définit `RequiredFields`, `OptionalFields`, `ForbiddenFields`, `Validation`, `ScoreImpact` et `StatisticalImpact`. Le formulaire affiche seulement les champs applicables.

## Recalcul

Après une écriture d'événement, `MatchRecalculationService` évalue score, chronologie, joueuses, gardiennes, zones, taux, sanctions, agrégats saison et caches. Le résultat est `RECALCULATED`, `UNCHANGED`, `FAILED`, `PARTIAL` ou `REQUIRES_MANUAL_REVIEW`.

## Temps de jeu

La donnée conserve source et confiance : `RECORDED_DIRECT`, `RECORDED_HISTORICAL_ID`, `MATCHED_STRONG_IDENTITY`, `MATCHED_UNIQUE_MATCH_ROSTER`, `DERIVED_FROM_SUBSTITUTIONS`, `PARTIAL_DATA`, `DATA_MISSING` ou `IDENTITY_CONFLICT`. `DATA_MISSING` reste vide et n'est pas converti en zéro.
