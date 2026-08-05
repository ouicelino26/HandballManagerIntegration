# Audit de sécurité actuel

## Verdict

Score actuel estimé : **35/100**. L'API et HTTPS constituent une bonne frontière, mais un secret client versionné, l'absence d'audit et les suppressions physiques empêchent toute qualification d'outil d'administration officiel.

## Constats

| ID | Sévérité | Constat | Impact | Action requise |
|---|---|---|---|---|
| SEC-01 | CRITICAL | Un secret client non vide est versionné dans `appsettings.json`. | Usurpation possible si le secret est encore accepté. | Rotation immédiate, suppression du client et de l'historique distribué. |
| SEC-02 | CRITICAL | L'archive de release suivie contient aussi cette configuration. | Secret distribué aux destinataires de l'archive. | Retirer/remplacer l'archive et republier un artefact assaini. |
| SEC-03 | HIGH | Aucune écriture métier n'alimente un audit complet. | Actions non attribuables, correction et enquête impossibles. | Audit serveur transactionnel avec utilisateur et correlationId. |
| SEC-04 | HIGH | Les suppressions API sont physiques et sans analyse d'impact. | Perte irréversible et rupture de dépendances. | Soft delete ou workflow transactionnel contrôlé. |
| SEC-05 | HIGH | Le rôle unique Admin concentre tous les droits. | Privilèges excessifs. | Six rôles et permissions serveur dédiées. |
| SEC-06 | HIGH | Les imports multi-entités ne sont pas transactionnels. | Base partiellement modifiée après erreur. | Endpoint preview/execute transactionnel. |
| SEC-07 | MEDIUM | Corps d'erreur et messages d'exception peuvent être affichés tels quels. | Fuite de détails internes. | Contrat ProblemDetails filtré et diagnostic copiable. |
| SEC-08 | MEDIUM | Les logs locaux contiennent des données métier sans rotation. | PII et chemins persistants sur le poste. | Journal structuré, rétention, redaction et dossier applicatif. |
| SEC-09 | MEDIUM | Pas de gestion centralisée de l'expiration JWT. | État UI incohérent après 401. | Handler HTTP, expiration proactive et retour login. |
| SEC-10 | MEDIUM | `POST /auth/register` est anonyme côté API. | Création publique de comptes Consultation. | Décision produit explicite, limitation/validation ou suppression. |
| SEC-11 | LOW | L'URL d'API cible est affichée sur le login. | Information technique inutile aux opérateurs. | Déplacer dans Paramètres/diagnostic autorisé. |

## Contrôles positifs

- Aucun accès direct MySQL dans le client WPF.
- Le mot de passe n'est pas persisté par le client.
- Le JWT reste en mémoire et est effacé au logout.
- Les routes d'écriture actuelles exigent le rôle Admin côté API.
- L'API utilise un hash salé pour les mots de passe.
- L'API ajoute un `correlationId` aux ProblemDetails génériques.

## Stockage du token cible

Un jeton de session non persistant peut rester en mémoire. Si un mécanisme « rester connecté » est ajouté, il devra utiliser Windows Data Protection/PasswordVault, une durée courte, une révocation et aucune journalisation. Un secret client partagé ne doit jamais être embarqué dans WPF.

## Contrôle de secrets Phase A

`SECRET_SCAN=FAIL`. Les valeurs n'ont pas été reproduites dans cette documentation. La rotation reste une action manuelle obligatoire avant Phase B.
