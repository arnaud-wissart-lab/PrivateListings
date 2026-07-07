# PrivateListings

[![CI](https://github.com/arnaud-wissart-lab/PrivateListings/actions/workflows/ci.yml/badge.svg)](https://github.com/arnaud-wissart-lab/PrivateListings/actions/workflows/ci.yml)

PrivateListings est une plateforme web responsive d'annonces privées et d'entraide locale. Elle permet à des membres d'une communauté de publier des annonces, proposer des services, demander de l'aide, organiser des événements, échanger via messagerie interne, et administrer ou modérer les contenus.

## Vision

Le produit vise une expérience sobre, fiable et sécurisée pour des communautés privées ou semi-privées : voisinage, associations, collectifs, entreprises, clubs ou groupes locaux. La priorité initiale est de livrer un MVP centré sur les annonces privées, l'entraide locale, la modération et la sécurité des comptes.

## Stack

- .NET 10 LTS
- ASP.NET Core / Blazor Web App
- .NET Aspire
- PostgreSQL
- EF Core 10
- Docker Desktop en développement
- Docker Compose pour le déploiement sur mini-PC GEEKOM
- GHCR pour les images Docker
- GitHub Actions CI/CD
- Dependabot
- Tests unitaires, d'intégration et fonctionnels
- UI française, responsive desktop et mobile

## Documentation

- [Vision produit](docs/PRODUCT.md)
- [Architecture](docs/ARCHITECTURE.md)
- [Sécurité](docs/SECURITY.md)
- [Confidentialité](docs/PRIVACY.md)
- [Tests E2E Playwright](docs/E2E.md)
- [Déploiement GEEKOM](docs/DEPLOYMENT.md)
- [Contribution](docs/CONTRIBUTING.md)
- [Règles projet Codex](AGENTS.md)

## Structure technique

Le squelette initial est généré dans `PrivateListings.sln` :

- `src/PrivateListings.AppHost` : orchestration Aspire locale.
- `src/PrivateListings.ServiceDefaults` : conventions Aspire partagées.
- `src/PrivateListings.Domain` : cœur métier sans dépendance infrastructure.
- `src/PrivateListings.Application` : cas d'usage et contrats applicatifs.
- `src/PrivateListings.Infrastructure` : EF Core, PostgreSQL et services techniques.
- `src/PrivateListings.Web` : interface Blazor Web App.
- `src/PrivateListings.Worker` : tâches planifiées.
- `tests/*` : tests unitaires, intégration PostgreSQL et E2E Playwright.

## Modules disponibles

- Authentification par email et mot de passe avec activation de compte.
- Reset password par lien temporaire à usage unique.
- Changement d'email par lien de confirmation envoyé à la nouvelle adresse.
- Annonces privées avec catégories, publication, expiration et images stockées hors webroot.
- Messagerie interne entre membres sur une annonce, notifications email sans contenu privé.
- Signalements, blocage de conversation et premiers écrans de modération.
- Back-office pour administrateurs et modérateurs : tableau de bord, annonces, catégories, utilisateurs, signalements et audit logs.
- Mode `Demo` isolé avec seed de comptes et données fictives.
- Worker pour l'expiration des annonces et la purge des jetons de sécurité expirés.
- Déploiement Docker Compose GEEKOM avec PostgreSQL, uploads et clés Data Protection persistants.

## Services de développement

L'AppHost Aspire démarre PostgreSQL et MailPit pour le développement local. Redis n'est pas configuré à ce stade, car aucun besoin de cache distribué, file distribuée ou coordination multi-instance n'est encore introduit.

## Commandes de démarrage

Docker Desktop doit être lancé avant le démarrage de l'AppHost, car PostgreSQL et MailPit sont exécutés en conteneurs.

```powershell
dotnet restore PrivateListings.sln
dotnet build PrivateListings.sln
dotnet test PrivateListings.sln
dotnet run --project src/PrivateListings.AppHost/PrivateListings.AppHost.csproj
```

La suite qualité locale complète est disponible via :

```powershell
.\scripts\test-all.ps1
```

Les parcours E2E Playwright sont exécutables séparément :

```powershell
.\scripts\e2e.ps1
.\scripts\e2e.ps1 -Headed
```

Les validations utilisées avant livraison publique sont :

```powershell
dotnet build PrivateListings.sln --configuration Release
dotnet test PrivateListings.sln --configuration Release
dotnet format PrivateListings.sln --verify-no-changes --no-restore
git diff --check
```

## Comptes démo

En environnement `Demo`, les comptes fictifs suivants sont créés si `PrivateListings:DemoSeed:Enabled=true` :

| Rôle | Email | Mot de passe |
| --- | --- | --- |
| Administrateur | `admin-demo@example.test` | `DemoAdmin!2026` |
| Modérateur | `moderateur-demo@example.test` | `DemoModerateur!2026` |
| Membre | `membre-demo@example.test` | `DemoMembre!2026` |

Ces comptes ne doivent jamais être utilisés en production et le seed démo est refusé par validation en environnement `Production`.

## État actuel

Le dépôt contient un MVP fonctionnel pour annonces privées et entraide locale. Les parcours principaux sont implémentés et couverts par tests unitaires, intégration PostgreSQL et E2E Playwright.

Limites actuelles :

- Le MFA administrateur reste à implémenter.
- Les rate limiters applicatifs sont en mémoire ; une implémentation distribuée sera nécessaire en multi-instance.
- Les images uploadées sont validées et stockées hors webroot, mais le redimensionnement/recompression n'est pas activé.
- Le module contact public n'est pas encore implémenté.
- Les sauvegardes chiffrées et les restaurations périodiquement testées restent à formaliser hors scripts existants.
- Le déploiement `Production` exige une configuration explicite : `PrivateListings:PublicBaseUrl`, `AllowedHosts`, SMTP et clés Data Protection persistantes.
