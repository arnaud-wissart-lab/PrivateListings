# Architecture

## Objectif

PrivateListings est une application .NET 10 LTS basée sur ASP.NET Core, Blazor Web App, .NET Aspire, PostgreSQL et EF Core 10. L'architecture doit rester maintenable, testable et déployable simplement par Docker Compose sur un mini-PC GEEKOM, tout en permettant une CI/CD propre vers GHCR.

Ce document décrit la cible de départ et le squelette technique initial généré.

## Principes d'architecture

- Architecture modulaire, lisible et évolutive.
- Séparation explicite entre UI, logique applicative, domaine et infrastructure.
- Dépendances orientées vers le domaine, pas vers les détails techniques.
- EF Core limité à l'infrastructure et aux tests d'intégration.
- Validation métier proche des cas d'utilisation.
- Configuration externalisée par environnement.
- Observabilité intégrée dès le départ via Aspire.
- Sécurité traitée comme une exigence transversale, pas comme un ajout tardif.

## Structure de solution cible

Structure prévue pour la solution `PrivateListings.sln` :

```text
src/
  PrivateListings.AppHost/
  PrivateListings.ServiceDefaults/
  PrivateListings.Web/
  PrivateListings.Application/
  PrivateListings.Domain/
  PrivateListings.Infrastructure/
  PrivateListings.Worker/
tests/
  PrivateListings.Domain.Tests/
  PrivateListings.Application.Tests/
  PrivateListings.IntegrationTests/
docs/
```

### `PrivateListings.AppHost`

Projet .NET Aspire chargé d'orchestrer l'environnement local :

- application web ;
- worker applicatif ;
- PostgreSQL ;
- MailPit pour les emails de développement ;
- variables de configuration non sensibles ;
- diagnostics et observabilité locale.

Redis n'est pas configuré dans le squelette initial. Il sera ajouté seulement si un besoin concret apparaît, par exemple cache distribué, coordination multi-instance, file de messages ou rate limiting distribué.

### `PrivateListings.ServiceDefaults`

Configuration partagée Aspire :

- health checks ;
- OpenTelemetry ;
- service discovery si nécessaire ;
- résilience HTTP ;
- conventions de logs.

### `PrivateListings.Web`

Application ASP.NET Core / Blazor Web App :

- pages et composants UI ;
- authentification par cookies ;
- endpoints ou handlers web ;
- protection CSRF ;
- validation d'entrée côté serveur ;
- intégration Metronic 9.5.0 si exploitable ;
- gestion responsive desktop et mobile.

La couche Web ne doit pas contenir de logique métier durable. Elle orchestre les cas d'utilisation de `PrivateListings.Application`.

### `PrivateListings.Application`

Couche des cas d'utilisation :

- commandes et requêtes applicatives ;
- validations applicatives ;
- contrats de services ;
- DTO internes ;
- règles d'autorisation applicative ;
- orchestration des transactions.

Elle ne dépend pas d'ASP.NET Core, d'EF Core ou de PostgreSQL.

### `PrivateListings.Domain`

Coeur métier :

- entités ;
- value objects ;
- règles invariantes ;
- événements de domaine si utiles ;
- énumérations métier.

Le domaine doit rester indépendant des détails de persistance, d'UI et d'infrastructure.

### `PrivateListings.Infrastructure`

Implémentations techniques :

- `DbContext` EF Core ;
- configurations de mapping ;
- migrations ;
- stockage des images hors webroot ;
- services de courrier électronique ;
- horloge système ;
- génération de jetons temporaires ;
- journal d'audit ;
- intégrations externes éventuelles.

### `PrivateListings.Worker`

Projet `BackgroundService` destiné aux tâches planifiées ou différées :

- nettoyages périodiques ;
- expirations de jetons temporaires ;
- traitements techniques sans interaction UI ;
- orchestration via les contrats applicatifs.

## Données

PostgreSQL est la base relationnelle cible.

### Domaine métier initial

Le projet `PrivateListings.Domain` porte les premières entités métier sans dépendance à EF Core, ASP.NET Core ou l'infrastructure :

- `UserProfile` : profil utilisateur, données affichables, anonymisation et historique de consentement.
- `Listing` : annonce appartenant à un utilisateur, statut, type, dates de publication et d'expiration.
- `ListingCategory` : catégorie d'annonce.
- `ListingImage` : métadonnées d'image associée à une annonce.
- `Conversation` : échange direct ou lié à une annonce.
- `Message` : message appartenant à une conversation.
- `Report` : signalement visant une annonce, un message ou un profil utilisateur.
- `Recommendation` : recommandation d'annonce pour un profil.
- `AuditLog` : trace d'action sensible.
- `ConsentRecord` : consentement historisé pour un profil.
- `DataExportRequest` : demande d'export des données personnelles.

Les comptes applicatifs, jetons temporaires et décisions de modération détaillées restent à concevoir dans les couches appropriées.

### EF Core

- La couche `PrivateListings.Infrastructure` contient `PrivateListingsDbContext`, les mappings explicites par entité et la migration initiale PostgreSQL.
- Les identifiants typés du domaine sont convertis en colonnes `uuid`.
- Les conversations stockent les participants dans une colonne PostgreSQL `uuid[]` indexée en GIN.
- Les cibles de signalement et sujets d'audit sont persistés sous forme de clés normalisées indexées.
- Les migrations sont versionnées dans `src/PrivateListings.Infrastructure/Persistence/Migrations`.
- Requêtes optimisées pour éviter les problèmes N+1.
- `AsNoTracking` par défaut pour les lectures sans modification.
- Transactions explicites pour les cas d'utilisation sensibles.
- Les tests d'intégration appliquent les migrations sur PostgreSQL réel via Testcontainers.

## Authentification et autorisation

Le MVP utilisera une authentification par cookies ASP.NET Core.

- Cookies `Secure`, `HttpOnly` et `SameSite`.
- Activation de compte par lien temporaire à usage unique.
- Reset password par lien temporaire à usage unique.
- Aucun mot de passe envoyé par email.
- Rate limiting sur login, inscription, reset password et contact.
- Rôles : membre, modérateur, administrateur.
- Politiques d'autorisation pour les actions sensibles.

## UI et Metronic

Metronic 9.5.0 peut être utilisé si les assets disponibles dans `E:\Assets\metronic-v9.5.0` sont compatibles avec Blazor Web App et la licence du projet.

Critères d'adoption :

- intégration claire dans le pipeline front-end ;
- absence de surcharge inutile ;
- composants accessibles ou facilement ajustables ;
- rendu responsive satisfaisant ;
- maintenance raisonnable.

Si Metronic n'est pas exploitable proprement, l'UI devra rester sobre, responsive et cohérente avec une approche ASP.NET Core / Blazor standard.

## Stockage des fichiers

Pour le MVP, seuls les uploads d'images sont autorisés.

- Stockage hors webroot.
- Renommage serveur systématique.
- Validation stricte du type MIME, de l'extension, de la taille et des dimensions.
- Suppression des métadonnées sensibles si possible.
- Accès aux images via endpoint contrôlé.
- Journalisation sans chemin sensible ni donnée personnelle inutile.

## Observabilité

- Logs structurés.
- Corrélation par requête.
- Health checks applicatifs et base de données.
- Traces OpenTelemetry via Aspire.
- Aucun secret ni donnée sensible dans les logs.
- Audit log séparé des logs techniques pour les actions sensibles.

## Tests

La stratégie de test doit être sérieuse dès le début :

- tests unitaires sur le domaine ;
- tests unitaires sur les cas d'utilisation ;
- tests d'intégration sur PostgreSQL ;
- tests web sur les parcours critiques ;
- tests de sécurité ciblés sur activation, reset, rate limiting, autorisations et CSRF ;
- tests de non-régression sur la modération et les suppressions.

## CI/CD

GitHub Actions devra couvrir :

- restore ;
- build ;
- tests ;
- format ou vérification de format ;
- analyse de vulnérabilités selon les outils retenus ;
- build d'images Docker ;
- publication sur GHCR ;
- génération ou validation des migrations si nécessaire.

Dependabot devra surveiller :

- packages NuGet ;
- actions GitHub ;
- images Docker ;
- dépendances front-end si un gestionnaire de paquets est ajouté.

## Déploiement

Le déploiement final cible Docker Compose sur mini-PC GEEKOM.

Services prévus :

- application web PrivateListings ;
- PostgreSQL ;
- reverse proxy si retenu ;
- volume persistant PostgreSQL ;
- volume persistant pour images privées ;
- configuration par variables d'environnement ;
- stratégie de sauvegarde documentée.

Les images applicatives seront publiées dans GHCR.

## Environnements

- `Development` : orchestration Aspire, Docker Desktop, données locales jetables.
- `Demo` : données fictives isolées, seed de démonstration activé, aucune donnée réelle.
- `Staging` : proche production, utilisé pour valider migrations et déploiement.
- `Production` : Docker Compose sur mini-PC GEEKOM, migrations et seed démo désactivés par défaut.

## Décisions à confirmer avant scaffold

- Modèle exact d'identité : ASP.NET Core Identity complet ou modèle applicatif plus ciblé.
- Niveau d'interactivité Blazor : SSR, Interactive Server, Interactive WebAssembly ou mixte.
- Utilisation effective de Metronic 9.5.0.
- Reverse proxy cible pour production.
- Stratégie de sauvegarde PostgreSQL et images.
- Outil de tests fonctionnels web.
