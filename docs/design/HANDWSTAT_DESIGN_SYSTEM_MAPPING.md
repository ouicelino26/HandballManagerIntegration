# Cartographie du design HandWStat vers WPF Admin

## Source analysée

Le dépôt `HANDBALLSTAT` est une application WPF .NET 8 utilisant MaterialDesignThemes. Son thème déclare LightBlue/Yellow, charge aussi DeepPurple/Lime, puis ajoute de nombreuses couleurs et tailles directement dans les Views. Il s'agit donc d'une identité visuelle implicite, pas d'un design system stable à copier tel quel.

## Motifs à conserver

| HandWStat actuel | Intention | Équivalent Admin WPF |
|---|---|---|
| Navigation latérale à icônes | Accès rapide aux grands espaces | Navigation repliable avec icône, libellé et permission |
| Barre supérieure de contexte | Identité et état global | En-tête de page + ScopeBar |
| Cartes Material | Regroupement lisible | Surface sans ombre excessive, bordure légère |
| DataGrid statistiques | Densité métier | Table virtualisée, filtrée et paginée |
| Couleur de performance | Lecture rapide | StatusBadge avec texte + icône + couleur |
| Dialogue de conflit joueuse | Résolution humaine | Centre de réconciliation avec comparaison |
| Thème clair et surfaces blanches | Lisibilité | Thème clair admin par défaut |

## Motifs à ne pas copier

- dimensions fixes et fenêtre non redimensionnable ;
- marges absolues et placement par coordonnées ;
- emoji comme icône fonctionnelle ;
- DeepPurple/Lime chargés en parallèle de LightBlue/Yellow ;
- couleurs nommées directement dans chaque écran ;
- tableaux sans pagination ni états de chargement ;
- navigation d'icônes sans libellés accessibles.

## Tokens WPF cibles

| Token | Valeur de départ | Usage |
|---|---|---|
| `AppBackgroundBrush` | `#F3F6F5` | Fond de fenêtre |
| `SurfaceBrush` | `#FFFFFF` | Pages et panneaux |
| `ElevatedSurfaceBrush` | `#F8FBFA` | Popovers et zones élevées |
| `PrimaryTextBrush` | `#172A25` | Titres et données |
| `SecondaryTextBrush` | `#52645E` | Libellés |
| `MutedTextBrush` | `#788680` | Aide et métadonnées |
| `AccentBrush` | `#247C70` | Action principale, continuité admin actuelle |
| `SuccessBrush` | `#2F7D5D` | Succès |
| `WarningBrush` | `#C58A1B` | Avertissement, rappel du jaune HandWStat |
| `DangerBrush` | `#B6493D` | Action dangereuse |
| `InfoBrush` | `#2879A8` | Information, rappel du bleu HandWStat |
| `BorderBrush` | `#D9E2DE` | Séparateurs |
| `FocusBrush` | `#0B6F82` | Focus clavier contrasté |

| Échelle | Valeurs |
|---|---|
| Espacement | `SpacingXS=4`, `SM=8`, `MD=16`, `LG=24`, `XL=32` |
| Rayons | `RadiusSM=6`, `MD=10`, `LG=16`, `XL=22` |
| Typographie | `Caption=12`, `Body=14`, `Subtitle=16`, `Title=24`, `Display=32` |

La police cible reste à valider visuellement. `Bahnschrift` peut porter les titres, avec `Segoe UI Variable` pour les données et formulaires afin d'améliorer la lecture et la disponibilité Windows.

## ResourceDictionary cible

```text
Themes/Colors.xaml
Themes/Typography.xaml
Themes/Spacing.xaml
Themes/Controls.xaml
Themes/Tables.xaml
Themes/Forms.xaml
Themes/Dialogs.xaml
Themes/Navigation.xaml
```

## Composants prioritaires

`AdminPageHeader`, `ScopeBar`, `SearchBar`, `FilterPanel`, `StatusBadge`, `MetricCard`, `DataGridToolbar`, `EmptyState`, `ErrorState`, `LoadingState`, `PermissionDeniedState`, `ConfirmationDialog`, `ImpactPreviewDialog`, `DiffViewer`, `EntityPicker`, `PaginationControl` et `ValidationSummary`.

## Règle de cohérence

HandWStat et l'administration doivent partager les mêmes codes sémantiques, formes de cartes, densité et vocabulaire. L'administration conserve toutefois une identité plus sobre et plus sûre : moins de décoration, plus de contexte, d'états et de preuves avant écriture.
