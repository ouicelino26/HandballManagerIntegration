# Design system et UX cible

## Direction

Une administration claire, calme et dense juste ce qu'il faut. Le shell conserve la qualité visuelle actuelle du client, adopte les repères sémantiques HandWStat et retire les cartes décoratives sans action.

## Navigation

- barre latérale repliable avec 12 modules ;
- icône, libellé, tooltip et nom accessible ;
- modules filtrés par permission ;
- scope actif visible dans l'en-tête ;
- recherche contextuelle, aide et session dans le shell ;
- route interne typée, sans switch de Views dans la fenêtre.

## Structure de page

```text
AdminPageHeader
ScopeBar
PrimaryAction + actions secondaires regroupées
SearchBar + FilterPanel repliable
ValidationSummary / ErrorState
DataGridToolbar
Contenu (table, détail ou workflow)
PaginationControl
```

## Workflow intégration

Les sept étapes `Source`, `Mapping`, `Validation`, `Prévisualisation`, `Décision`, `Exécution`, `Rapport` restent visibles. L'opérateur peut revenir avant exécution, mais une modification de source invalide la preview. Une seule action principale est affichée par étape.

## Tables

- tri, filtres et pagination serveur ;
- virtualisation WPF ;
- sélection explicite ;
- menu d'actions par ligne ;
- export selon permission ;
- colonnes essentielles par défaut ;
- états loading, empty, error et permission denied ;
- densité confortable/compacte comme préférence locale.

## Formulaires

- labels permanents, jamais uniquement placeholder ;
- aide et contraintes avant erreur ;
- validation immédiate puis résumé ;
- indicateur de modifications ;
- `Annuler` et `Enregistrer` stables ;
- prévention de fermeture si données non sauvées ;
- diff/impact avant action sensible.

## Dialogues dangereux

Le bouton dangereux est séparé, rouge uniquement pour l'action, jamais préselectionné. Le dialogue affiche entité, dépendances, motif et version. La confirmation simple oui/non actuelle est interdite pour les suppressions métier.

## États et mouvement

Les animations servent la compréhension : apparition de page, progression d'étape et ouverture du panneau de filtres. Pas de faux pourcentage. `ReducedMotion` doit pouvoir désactiver les transitions non essentielles.

## Definition of Done UI

Tokens, navigation, loading, empty, error, permission, clavier, focus, contraste, redimensionnement, performance et revue visuelle doivent tous être validés.
