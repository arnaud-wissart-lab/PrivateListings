# Règles projet pour Codex

## Langue

- Toutes les réponses adressées au mainteneur doivent être rédigées en français.
- Les plans, hypothèses, explications, revues de code, résumés et comptes rendus doivent être en français.
- Les commandes, noms de fichiers, identifiants techniques, messages d'erreur, logs, URLs et extraits de code restent dans leur forme d'origine.
- Les commentaires, docstrings, fichiers de documentation et notes techniques créés pour ce dépôt doivent être en français, sauf convention locale contraire.

## Périmètre actuel

- Le projet s'appelle `PrivateListings`.
- Le dépôt cible s'appelle `private-listings`.
- La solution .NET cible s'appellera `PrivateListings`.
- Le namespace racine cible est `PrivateListings`.
- La langue UI initiale est le français.
- La baseline produit est : annonces privées et entraide locale.

## Mode opératoire

- Vérifier l'état du dépôt avant modification.
- Lire les conventions existantes avant de modifier le code.
- Garder des diffs petits, ciblés et faciles à relire.
- Ne pas générer toute l'application sans demande explicite.
- Ne pas introduire d'abstraction spéculative.
- Documenter les décisions structurantes lorsqu'elles engagent la suite du projet.
- Signaler clairement les validations exécutées et celles non exécutées.

## Stack cible

- .NET 10 LTS.
- ASP.NET Core / Blazor Web App.
- .NET Aspire.
- PostgreSQL.
- EF Core 10.
- Docker Desktop en développement.
- Docker Compose pour le déploiement final sur mini-PC GEEKOM.
- GHCR pour les images Docker.
- GitHub Actions CI/CD.
- Dependabot.
- Tests automatisés sérieux.
- Interface responsive desktop et mobile.
- Metronic 9.5.0 si exploitable depuis `E:\Assets\metronic-v9.5.0`.

## Architecture attendue

- Architecture propre et maintenable.
- Séparation entre `Web`, `Application`, `Domain` et `Infrastructure`.
- Domaine indépendant d'ASP.NET Core, EF Core et PostgreSQL.
- Infrastructure responsable de la persistance, des emails, du stockage fichier et des intégrations techniques.
- Cas d'utilisation testables sans UI.
- Migrations EF Core versionnées.
- Observabilité via Aspire et logs structurés.

## Sécurité

- Aucun mot de passe envoyé par email.
- Activation de compte par lien temporaire à usage unique.
- Reset password sécurisé par lien temporaire à usage unique.
- Rate limiting sur login, inscription, reset password et contact.
- Cookies `Secure`, `HttpOnly` et `SameSite`.
- Protection CSRF sur les actions qui modifient l'état.
- Pas de HTML libre dans les annonces.
- Upload images uniquement en MVP.
- Validation stricte des uploads.
- Renommage serveur des fichiers.
- Stockage des fichiers hors webroot.
- Logs sans données sensibles.
- Audit log pour actions admin et actions sensibles.
- Suppression et anonymisation documentées.
- Mode démo isolé avec fausses données.

## Qualité .NET

- Privilégier l'asynchrone de bout en bout pour les chemins I/O-bound.
- Passer les `CancellationToken` aux frontières asynchrones lorsque c'est pertinent.
- Respecter les durées de vie de l'injection de dépendances.
- Éviter le service locator.
- Surveiller avec EF Core la forme des requêtes, le tracking, les migrations et les risques N+1.
- Garder des frontières explicites entre contrats, DTO et domaine.
- Respecter la nullabilité.
- Éviter les conversions implicites risquées.

## Qualité front-end

- Construire des composants petits, typés et accessibles.
- Préserver la lisibilité mobile et desktop.
- Traiter explicitement les états de chargement, d'erreur, vide et succès.
- Éviter l'état inutile et les effets superflus.
- Garder les clients API explicites.
- Ne pas afficher de données privées à un rôle non autorisé.

## Tests et validation

- Exécuter les builds, tests, linters et formatters pertinents lorsqu'ils existent.
- Ajouter des tests dès qu'une règle métier, sécurité ou modération est introduite.
- Prévoir des tests d'intégration avec PostgreSQL réel pour la persistance.
- Prévoir des tests ciblés sur activation, reset password, rate limiting, CSRF, autorisations et uploads.
- Si une validation ne peut pas être exécutée, expliquer précisément pourquoi.

## Git

- Ne jamais utiliser `git add .` lorsque le jeu de modifications est mixte.
- Ne jamais réécrire ou supprimer des changements utilisateur sans demande explicite.
- Garder des commits atomiques.
- Rédiger les messages de commit au format Conventional Commits en français.
- Ne jamais mentionner l'IA, Codex ou un outil de génération dans les commits, PR ou commentaires de revue.

## Documentation

- Maintenir `docs/PRODUCT.md`, `docs/ARCHITECTURE.md`, `docs/SECURITY.md` et `docs/PRIVACY.md` à jour quand une décision les impacte.
- Éviter les promesses vagues ou les informations non vérifiées.
- Distinguer clairement ce qui est implémenté de ce qui est prévu.
