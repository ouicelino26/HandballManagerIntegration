# Audit UI/UX actuel

## Synthèse

Score actuel estimé : **58/100**. La direction visuelle du client est plus cohérente que l'application HandWStat historique, mais l'expérience reste un ensemble de cinq écrans techniques, sans états communs, permissions fines ni workflows administratifs complets.

## Écrans

| Écran | Points utiles | Limites principales | Statut UX |
|---|---|---|---|
| Login | Hiérarchie claire, focus initial, état occupé | URL technique visible, erreurs internes, aucune aide d'expiration | PARTIAL |
| Shell | Barre latérale repliable, session visible | Navigation codée en dur, cinq modules seulement, pas de scope | PARTIAL |
| Dashboard | API et quelques métriques | Charge toutes les joueuses, KPI non actionnables, pas d'anomalies | PARTIAL |
| Intégrations | Saison/journée par fichier, deux onglets, statuts | Pas de workflow, preview, validation groupée ou rapport | PARTIAL |
| Joueuses | Recherche, édition complète, statut actif | Recherche locale, boutons de ligne visibles, suppression dangereuse | PARTIAL |
| Comptes | Création et liste simples | Pas d'édition, désactivation, permissions ni pagination | PARTIAL |
| Studio PDF | Manipulation visuelle de blocs | Démonstrateur non fonctionnel avec données fictives | LEGACY |

## Design system actuel

Un dictionnaire unique fournit une palette chaude vert/terre cuite, la police Bahnschrift, des cartes, boutons, champs et DataGrid. Les styles de navigation, formulaires et dialogues sont encore redéfinis localement. Les tokens d'espacement, rayons et typographie ne sont pas nommés.

## Cohérence et densité

- Hiérarchie visuelle globalement lisible.
- Plusieurs cartes KPI occupent de la place sans conduire à une action.
- Les DataGrid mélangent édition et suppression directement dans chaque ligne.
- Les pages ont leurs propres variantes de styles et microcopies.
- La navigation ne couvre pas les 12 modules cibles.
- Les filtres ne sont ni repliables ni persistants.
- Aucun composant partagé pour loading, empty, error ou permission denied.

## Accessibilité

- Presque aucun `AutomationProperties.Name` ou `HelpText`.
- Focus visible non défini explicitement.
- Ordre clavier et raccourcis non documentés.
- Les statuts dépendent fortement de la couleur et de textes courts.
- Les dialogues ne portent pas de résumé d'erreurs annoncé.
- Les boutons d'icône du shell HandWStat historique montrent la nécessité d'un libellé accessible.

## Performance perçue

- La page joueuses charge toutes les pages API avant affichage.
- Le dashboard refait un chargement complet des joueuses.
- La recherche filtre à chaque frappe sans délai.
- Les imports ne supportent pas l'annulation.
- La progression correspond à des messages d'étape, pas à une mesure vérifiable.

## Recommandations

1. Construire d'abord les tokens et composants d'état partagés.
2. Remplacer la navigation en code-behind par un registre de modules et permissions.
3. Transformer l'import en assistant à sept étapes.
4. Adopter recherche, tri et pagination serveur pour les listes.
5. Remplacer les boutons de ligne par un menu d'actions.
6. Rendre chaque KPI cliquable vers une liste filtrée.
7. Ajouter labels accessibles, focus, clavier et tests UI Automation.
