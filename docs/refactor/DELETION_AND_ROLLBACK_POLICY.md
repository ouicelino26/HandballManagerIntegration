# Politique de suppression et rollback

## Principe

La préférence est l'archivage ou le soft delete. Une suppression physique n'est autorisée que si l'entité et toutes ses dépendances possèdent une stratégie transactionnelle, d'audit et de restauration documentée.

## Workflow obligatoire

1. Sélection explicite de l'entité.
2. Chargement serveur de l'analyse d'impact.
3. Affichage des dépendances et recalculs.
4. Saisie d'un motif obligatoire.
5. Confirmation forte avec libellé de l'entité.
6. Vérification de la permission et de la version.
7. Transaction d'archivage/suppression et audit.
8. Recalcul des agrégats.
9. Rapport et identifiant d'audit.

## Impact match

L'analyse couvre événements, temps de jeu, statistiques, fichiers/imports, historiques, exports, zones, caches et agrégats saison. Elle retourne compte, mode (`ARCHIVE`, `SOFT_DELETE`, `HARD_DELETE`), éléments bloquants et capacité de rollback.

## Impact événement

L'analyse couvre score, ordre chronologique, statistiques joueuse/gardienne, zones, sanctions et agrégats. La suppression déclenche le même moteur de recalcul qu'une modification.

## Requête de suppression

```text
Reason
ExpectedVersion
DeletionMode
CorrelationId
DryRun
```

## Réponse

```text
DeletedEntities
ArchivedEntities
RecalculatedScopes
Warnings
AuditId
RollbackToken (si supporté)
```

## Critères de passage

`IMPACT_PREVIEW`, `AUTHORIZATION`, `CONFIRMATION`, `REASON_REQUIRED`, `TRANSACTION`, `AUDIT`, `RECALCULATION`, `ROLLBACK_STRATEGY` et `TESTS` doivent tous être `PASS`.

## État actuel

Les DELETE de match, événement, joueuse et utilisateur sont physiques, sans motif ni audit. Ils ne doivent pas être exposés dans la nouvelle UI avant la livraison des endpoints contrôlés.
