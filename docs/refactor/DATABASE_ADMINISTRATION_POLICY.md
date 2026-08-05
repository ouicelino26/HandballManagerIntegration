# Politique d'administration des données

## Mode d'accès

`CURRENT_DATABASE_ACCESS_MODE=API_ONLY`

`TARGET_DATABASE_ACCESS_MODE=API_ONLY_WITH_ADMIN_SERVICES`

Le client WPF ne reçoit aucune connection string MySQL. Toute fonctionnalité durable passe par une route métier autorisée.

## Entités administrables

Une entité n'est exposée que si elle possède une définition serveur :

```text
EntityCode
Label
Permissions
SearchFields
EditableFields
RequiredFields
DeletePolicy
AuditPolicy
DefaultSort
Dependencies
```

Les tables système, secrets, migrations et tables non allow-listées ne sont jamais accessibles dans l'explorateur.

## Règles d'écriture

- validation de forme et métier ;
- autorisation dédiée ;
- transaction proportionnée ;
- audit dans la même transaction ;
- concurrence optimiste ;
- motif obligatoire pour correction sensible ;
- aucune donnée manquante remplacée par zéro ;
- aucune intégrité masquée par une valeur par défaut technique.

## Données historiques

L'équipe courante d'une joueuse peut évoluer, mais les événements de match conservent l'équipe au moment du match. Un transfert doit donc mettre à jour la joueuse et créer un historique explicite, sans réécrire les événements passés.

## Fichiers et rétention

- calcul du SHA-256 avant upload ;
- nom de fichier normalisé et taille/type vérifiés ;
- stockage brut optionnel, chiffré et à durée limitée ;
- rapport et métadonnées conservés selon politique ;
- suppression automatique des fichiers temporaires ;
- aucune charge complète dans les logs d'audit.

## Production

Les diagnostics directs sont lecture seule, séparés et interdits dans cette mission. Aucune migration, suppression ou écriture de production n'a été réalisée.
