# Contribuer à PrivateListings

Ce document décrit les règles minimales pour proposer une modification maintenable, vérifiable et sûre.

## Commandes locales

Depuis la racine du dépôt :

```powershell
dotnet restore PrivateListings.sln
dotnet build PrivateListings.sln --configuration Release --no-restore
dotnet format PrivateListings.sln --verify-no-changes --no-restore --verbosity normal
dotnet test PrivateListings.sln --configuration Release --no-build
```

Le script local regroupe ces vérifications :

```powershell
.\scripts\test-all.ps1
```

Les tests d'intégration utilisent PostgreSQL via Testcontainers. Docker Desktop doit être lancé avant d'exécuter toute suite qui dépend de ces tests.

Pour lancer l'application en développement :

```powershell
dotnet run --project src/PrivateListings.AppHost/PrivateListings.AppHost.csproj
```

## Branches et pull requests

- Créer une branche courte et descriptive, par exemple `feat/listing-search` ou `fix/login-rate-limit`.
- Garder les pull requests centrées sur une intention unique.
- Décrire le changement, la raison, les validations exécutées et les risques résiduels.
- Ajouter ou mettre à jour la documentation lorsque la modification change une décision produit, architecture, sécurité ou confidentialité.
- Ne pas mélanger refactorisation large et changement fonctionnel sans justification claire.

## Exigences de tests

- Tout changement métier doit être couvert par des tests unitaires ou applicatifs ciblés.
- Tout changement de persistance doit inclure une validation d'intégration pertinente avec PostgreSQL réel.
- Les parcours d'authentification, d'autorisation, de rate limiting, de CSRF, d'upload et de modération doivent conserver une couverture dédiée.
- La CI doit rester verte : restauration, compilation Release, vérification du format, tests et builds Docker doivent réussir.
- Les avertissements importants doivent être corrigés à la source plutôt que masqués.

## Secrets et configuration

- Aucun secret ne doit être commité : mot de passe, jeton, clé API, chaîne de connexion réelle, certificat privé ou fichier `.env`.
- Les fichiers `.env` sont ignorés par Git. Seuls les exemples sans secret, comme `.env.example`, sont autorisés si un besoin local est documenté.
- Utiliser les secrets utilisateurs .NET, les variables d'environnement locales ou les secrets GitHub Actions selon le contexte.
- Les images Docker de production ne doivent pas contenir de données de démonstration en dur ni de secrets passés en `ARG`.

## Revue sécurité minimale

Avant de demander une revue, vérifier au minimum :

- les entrées utilisateur sont validées et les messages d'erreur ne divulguent pas de données sensibles ;
- les changements d'état restent protégés contre les accès non autorisés et les attaques CSRF ;
- les logs ne contiennent pas de secrets, jetons, mots de passe, adresses privées inutiles ou données sensibles ;
- les uploads restent limités aux formats attendus, renommés côté serveur et stockés hors webroot ;
- les migrations et seeds ne créent pas de compte réel, de mot de passe ou de donnée de démonstration en production ;
- les dépendances ajoutées sont nécessaires, maintenues et compatibles avec la licence du projet.
