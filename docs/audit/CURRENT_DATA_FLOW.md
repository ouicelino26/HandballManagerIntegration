# Flux de données actuels

## Authentification

```text
LoginWindow
  -> POST /auth/login (identifiant + mot de passe)
  -> ApiAuthService garde le JWT en mémoire
  -> contrôle local Role == Admin
  -> GET /api/Users/me
  -> MainWindow
```

Le token n'est pas écrit dans un fichier. Il n'existe ni refresh token, ni expiration proactive, ni gestion centralisée des réponses 401/403.

## Import match et événements

```text
Dossier choisi
  -> recherche récursive des fichiers XLSX nommés selon convention
  -> date initiale = date du poste
  -> saison courante et journée déduite du chemin
  -> conversion XLSX vers CSV à côté du fichier source
  -> mapping CsvHelper vers MatchFileDto
  -> résolution équipes par nom
  -> résolution joueuses exacte puis approximative
  -> création éventuelle d'une joueuse
  -> préparation des événements en mémoire
  -> comparaison d'un match candidat et de ses événements
  -> mise à jour des équipes des joueuses
  -> POST match
  -> POST de chaque événement
  -> logs texte locaux pour les lignes ignorées/échouées
```

### Ruptures d'intégrité

- Les changements d'équipe sont exécutés avant la création du match.
- Le match et les événements ne partagent pas de transaction.
- Un événement rejeté est journalisé puis l'import continue.
- L'écran peut afficher un succès alors que des événements ont échoué.
- Aucun rollback ne retire un match incomplet.
- Un événement inconnu est remplacé par l'identifiant numérique `37`.
- Le score final absent est remplacé par zéro.
- Le CSV intermédiaire reste dans le dossier source.

### Idempotence actuelle

La recherche compare compétition, date, saison, journée, équipes, score et empreintes des événements. Elle ne conserve ni SHA-256 du fichier, ni signature métier persistée, ni version du mapping. Un match proche mais non identique n'est pas présenté comme conflit.

## Import temps de jeu

```text
XLSX
  -> équipes déduites du dossier puis de la feuille
  -> recherche d'un match existant par saison + journée + 2 équipes
  -> refus si au moins une ligne TimePlayers existe déjà
  -> lecture de Feuil1/feuille 2
  -> résolution joueuse exacte/approximative
  -> POST /api/TimePlayers pour chaque ligne
  -> résumé importé/ignoré dans l'écran
```

Il n'existe pas de transaction client. L'API possède aussi un endpoint d'import XLSX plus cohérent, mais le client ne l'utilise pas. Une erreur après plusieurs lignes laisse les lignes précédentes en base.

## Gestion joueuses

```text
GET /api/Players paginé par lots de 500
  -> agrégation de toutes les pages dans le client
  -> recherche et tri en mémoire
  -> PUT partiel pour modification/statut/équipe
  -> DELETE physique après confirmation simple
```

La liste ne bénéficie pas de pagination, recherche ou annulation côté UI. Le prénom et le nom sont reconstruits depuis `FullName`, ce qui n'est pas fiable pour les noms composés.

## Gestion utilisateurs

```text
GET /api/Users
  -> affichage complet
POST /api/Users
  -> création Admin ou Consultation
```

Le client ne modifie ni ne désactive un compte existant. Aucun changement n'est audité.

## Données sensibles et traces

- Mot de passe : présent uniquement pendant la saisie et la requête de login/création.
- JWT : mémoire du processus uniquement.
- Secret client : versionné dans la configuration et dans l'archive de release.
- Logs locaux : noms de joueuses, équipes, chemins et charges d'événements possibles, sans rotation ni rétention.
- Fichiers source : lus directement, sans copie contrôlée, hash ou politique de rétention.

## Flux cible

Le client doit envoyer le fichier et son contexte à un endpoint `preview`, recevoir un plan immuable, puis transmettre son `PreviewId`, sa décision et sa version attendue à `execute`. L'API doit effectuer validation, transaction, audit, idempotence et rollback, puis retourner un rapport structuré.
