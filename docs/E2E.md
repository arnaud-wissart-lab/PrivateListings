# Tests E2E Playwright

Le projet `tests/PrivateListings.E2ETests` couvre les parcours navigateur principaux de PrivateListings avec Playwright pour .NET.

## Périmètre couvert

- visiteur : landing page et page `Données & sécurité` ;
- membre démo : connexion, liste des annonces, filtres, détail, création de brouillon, publication, réponse à une annonce, messagerie et envoi de message ;
- signalement : signalement d'un message depuis une conversation ;
- sécurité : accès admin refusé à un membre, conversation d'un autre membre inaccessible, contenu des messages absent des emails de notification, upload invalide refusé ;
- modération : consultation du back-office et résolution d'un signalement ;
- admin : consultation des audit logs.

## Isolation

Par défaut, les tests ne ciblent jamais une base existante :

- PostgreSQL est lancé via Testcontainers avec une base jetable ;
- la base est supprimée puis reconstruite avant chaque test ;
- le seed `Demo` fournit des comptes et données prévisibles ;
- l'application Web est lancée en HTTPS local avec un certificat généré pour le test ;
- les emails passent par un faux serveur SMTP local en mémoire ;
- les images téléversées pendant les tests sont écrites sous `output/playwright/`.

Les comptes démo utilisés par les tests sont :

| Rôle | Email | Mot de passe |
| --- | --- | --- |
| Admin | `admin-demo@example.test` | `DemoAdmin!2026` |
| Modérateur | `moderateur-demo@example.test` | `DemoModerateur!2026` |
| Membre | `membre-demo@example.test` | `DemoMembre!2026` |

## Exécution locale

Docker Desktop doit être lancé avant l'exécution, car PostgreSQL est démarré par Testcontainers.

Exécution headless par défaut :

```powershell
.\scripts\e2e.ps1
```

Exécution avec navigateur visible :

```powershell
.\scripts\e2e.ps1 -Headed
```

Exécution d'un test ciblé :

```powershell
.\scripts\e2e.ps1 -Filter "FullyQualifiedName~MemberCanCreatePublishReplyAndSendMessage"
```

La suite qualité complète peut inclure les E2E avec :

```powershell
.\scripts\test-all.ps1 -IncludeE2E
```

## Artefacts d'échec

En cas d'échec, le harnais écrit :

- une capture d'écran pleine page ;
- une trace Playwright consultable avec `playwright show-trace`.

Les artefacts sont placés dans `output/playwright/<horodatage>/`. La CI publie ces fichiers si la suite E2E échoue.

## CI

La CI exécute les E2E dans un job séparé. Le job installe Chromium avec les dépendances système Playwright, démarre PostgreSQL via Testcontainers, lance l'application Web en mode `Demo`, puis exécute uniquement les tests marqués `Category=E2E`.

Le job standard exclut ces tests avec `Category!=E2E` pour éviter de dépendre des navigateurs Playwright lors des validations unitaires et d'intégration classiques.
