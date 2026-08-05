# Matrice d'accessibilité Admin

| Zone | Critère | État actuel | Cible | Vérification |
|---|---|---|---|---|
| Shell | Navigation clavier complète | PARTIAL | Tous les modules accessibles sans souris | Test UI Automation |
| Shell | Libellé des icônes | MISSING | `AutomationProperties.Name` et tooltip | Inspection Accessibility Insights |
| Shell | Focus visible | MISSING | Anneau `FocusBrush` 2 px | Revue clavier |
| Pages | Ordre du focus | NOT_VERIFIED | Ordre logique titre, filtres, table, actions | Parcours Tab/Shift+Tab |
| Formulaires | Label associé | PARTIAL | Label explicite et `LabeledBy` | UI Automation |
| Formulaires | Aide contextuelle | PARTIAL | `HelpText` sur formats/contraintes | Inspection |
| Formulaires | Erreur annoncée | MISSING | Résumé + live region | Test lecteur d'écran |
| Formulaires | Modifications non sauvées | MISSING | Avertissement accessible | Test fonctionnel |
| Tableaux | Navigation clavier | PARTIAL | Cellules, lignes, menu d'actions | Test clavier |
| Tableaux | Tri annoncé | PARTIAL | Nom et direction accessibles | UI Automation |
| Tableaux | État vide | MISSING | Message et action suivante | Test ViewModel |
| Tableaux | État chargement | PARTIAL | Libellé non bloquant et annulation | Test UI |
| Statuts | Information hors couleur | PARTIAL | Texte + icône + couleur | Revue contraste |
| Dialogues | Titre et focus initial | PARTIAL | Titre annoncé, focus sur action sûre | Test lecteur d'écran |
| Dialogues | Retour du focus | NOT_VERIFIED | Retour au déclencheur | Test clavier |
| Actions dangereuses | Confirmation compréhensible | BROKEN | Entité, impact, motif, libellé explicite | Test scénario |
| Zoom | Mise à l'échelle Windows | NOT_VERIFIED | 125 %, 150 %, 200 % sans perte | Revue visuelle |
| Contraste | Texte et contrôles | NOT_VERIFIED | WCAG AA, 4.5:1 texte normal | Analyse automatique |
| Taille cible | Boutons et menus | PARTIAL | Minimum 40 px pour action principale | Revue XAML |
| Raccourcis | Documentation | MISSING | Aide clavier contextuelle | Test documentation |

## Raccourcis cibles

| Raccourci | Action |
|---|---|
| `Ctrl+K` | Recherche globale/contextuelle |
| `Ctrl+F` | Recherche dans la liste active |
| `Ctrl+R` | Actualiser |
| `Ctrl+S` | Enregistrer un formulaire |
| `Esc` | Fermer/annuler sans écriture |
| `Alt+Left` | Retour à la liste |

L'accessibilité fait partie de la Definition of Done de chaque composant, pas d'une correction finale.
