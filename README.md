# PrivateListings

[![CI](https://github.com/arnaud-wissart-lab/PrivateListings/actions/workflows/ci.yml/badge.svg)](https://github.com/arnaud-wissart-lab/PrivateListings/actions/workflows/ci.yml)

PrivateListings est une plateforme web responsive d'annonces privées et d'entraide locale. Elle permet à des membres d'une communauté de publier des annonces, proposer des services, demander de l'aide, organiser des événements, échanger via messagerie interne, et administrer ou modérer les contenus.

## Vision

Le produit vise une expérience sobre, fiable et sécurisée pour des communautés privées ou semi-privées : voisinage, associations, collectifs, entreprises, clubs ou groupes locaux. La priorité initiale est de livrer un MVP centré sur les annonces privées, l'entraide locale, la modération et la sécurité des comptes.

## Stack prévue

- .NET 10 LTS
- ASP.NET Core / Blazor Web App
- .NET Aspire
- PostgreSQL
- EF Core 10
- Docker Desktop en développement
- Docker Compose pour le déploiement final sur mini-PC GEEKOM
- GHCR pour les images Docker
- GitHub Actions CI/CD
- Dependabot
- Tests unitaires, d'intégration et fonctionnels
- UI française, responsive desktop et mobile
- Metronic 9.5.0 si exploitable depuis `E:\Assets\metronic-v9.5.0`

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
- `tests/*` : tests de structure initiaux.

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

## État actuel

Le dépôt contient le squelette technique .NET 10 initial, compilable, sans code métier complexe.
