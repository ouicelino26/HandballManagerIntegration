# Vision produit HandWStat Admin

HandballManagerIntegration devient le poste de contrôle officiel des données HandWStat. Il guide un opérateur depuis une source brute jusqu'à une donnée validée, expliquée, auditable et réversible.

## Promesse

- Comprendre avant d'écrire.
- Montrer les conséquences avant une correction ou une suppression.
- Ne jamais confondre donnée absente et valeur zéro.
- Rendre chaque anomalie actionnable.
- Attribuer chaque écriture à un utilisateur et un motif.
- Permettre la reprise sans créer de doublon.

## Utilisateurs

| Profil | Besoin principal |
|---|---|
| Administrateur fonctionnel | Référentiels, droits et arbitrages |
| Analyste vidéo | Import, validation et correction d'événements |
| Responsable des données | Qualité, réconciliation et audit |
| Opérateur d'intégration | Workflow guidé et rapport clair |
| Support technique | Diagnostic corrélé sans secrets |
| Administrateur système autorisé | Maintenance contrôlée et versionnement |

## Principes produit

1. L'API est l'autorité pour droits, validation, audit et transaction.
2. Toute écriture importante commence par une simulation.
3. Une opération partielle est un état explicite, jamais un succès.
4. Les identités ne sont pas fusionnées par simple ressemblance.
5. Les actions dangereuses sont rares, séparées et fortement confirmées.
6. Chaque dashboard mène à une liste filtrée ou une action.
7. Les termes et microcopies sont en français métier.

## Indicateurs de réussite

- zéro doublon exact silencieux ;
- zéro import affiché réussi avec des écritures échouées ;
- 100 % des écritures administratives auditées ;
- 100 % des suppressions couvertes par impact, motif et transaction ;
- listes principales paginées côté serveur ;
- couverture des cas métier P0 avant ouverture à plusieurs rôles ;
- parcours clavier complet sur les workflows principaux.

## Hors périmètre initial

- éditeur SQL libre ;
- accès direct MySQL depuis WPF ;
- exposition automatique de toutes les tables ;
- modification de la production depuis un outil de développement ;
- refonte simultanée de HandWStat analytique.
