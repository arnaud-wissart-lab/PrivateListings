# Déploiement GEEKOM

## Objectif

PrivateListings est déployé sur le mini-PC GEEKOM avec Docker Compose, sans imposer de reverse proxy applicatif dans ce dépôt. Les images applicatives sont publiées sur GHCR, puis tirées par le GEEKOM.

Services prévus :

- `privatelistings-web` : application ASP.NET Core / Blazor.
- `privatelistings-worker` : tâches planifiées, notamment expiration des annonces.
- `privatelistings-postgres` : base PostgreSQL persistante.
- `privatelistings-mailpit` : service email optionnel, uniquement avec le profil Compose `nonproduction`.

## Fichiers

- `deploy/docker-compose.geekom.yml` : orchestration Docker Compose cible GEEKOM.
- `deploy/.env.example` : modèle de configuration, sans secret réel.
- `deploy/scripts/deploy.ps1` : déploiement local ou distant par SSH.
- `deploy/scripts/backup-db.ps1` : sauvegarde PostgreSQL au format custom `pg_dump`.
- `deploy/scripts/restore-db.ps1` : restauration PostgreSQL.
- `.github/workflows/deploy.yml` : workflow manuel de build, publication GHCR et déploiement SSH optionnel.

## Préparation du GEEKOM

Créer le dossier de déploiement sur le GEEKOM :

```bash
sudo mkdir -p /opt/privatelistings
sudo chown "$USER":"$USER" /opt/privatelistings
```

Copier sur le GEEKOM :

- `deploy/docker-compose.geekom.yml` vers `/opt/privatelistings/docker-compose.geekom.yml`.
- `deploy/.env.example` vers `/opt/privatelistings/.env`, puis remplacer les valeurs.

Ne jamais copier un `.env` réel dans Git. Le fichier `deploy/.env.example` doit rester le seul fichier d'environnement versionné.

## Images GHCR

Images attendues :

- `ghcr.io/<owner>/private-listings-web`
- `ghcr.io/<owner>/private-listings-worker`

Dans `/opt/privatelistings/.env`, renseigner par exemple :

```env
PRIVATE_LISTINGS_WEB_IMAGE=ghcr.io/arnaud-wissart-lab/private-listings-web
PRIVATE_LISTINGS_WORKER_IMAGE=ghcr.io/arnaud-wissart-lab/private-listings-worker
PRIVATE_LISTINGS_IMAGE_TAG=latest
```

Le propriétaire GHCR doit être en minuscules.

## Volumes persistants

Volumes Docker nommés :

- `privatelistings_postgres_data` : données PostgreSQL.
- `privatelistings_uploads` : fichiers uploadés, notamment les images d'annonces.

Docker Compose peut les créer automatiquement. Pour les créer explicitement :

```bash
docker volume create privatelistings_postgres_data
docker volume create privatelistings_uploads
```

Les uploads sont montés dans les conteneurs applicatifs sous :

```text
/var/lib/privatelistings/uploads
```

L'application stocke les images dans `listing-images` sous ce volume. Le reverse proxy ne doit pas servir ce volume directement : les images doivent rester exposées par les endpoints applicatifs, qui appliquent les contrôles d'accès.

## Configuration production

Paramètres importants dans `.env` :

```env
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
PRIVATE_LISTINGS_APPLY_MIGRATIONS_ON_STARTUP=false
PRIVATE_LISTINGS_DEMO_SEED_ENABLED=false
PRIVATE_LISTINGS_EXPOSE_HEALTH_ENDPOINTS=true
```

Le compose publie le web sur `127.0.0.1:8080` par défaut. Cela suppose un reverse proxy externe sur le GEEKOM ou sur l'infrastructure locale. Le reverse proxy doit gérer TLS et transmettre les en-têtes `X-Forwarded-Proto` et `X-Forwarded-For`.

## Healthchecks

PostgreSQL :

- `pg_isready` sur la base configurée.

Web :

- endpoint `/alive` exposé par `PrivateListings.ServiceDefaults` lorsque `PRIVATE_LISTINGS_EXPOSE_HEALTH_ENDPOINTS=true`.
- le healthcheck Docker appelle `http://127.0.0.1:8080/alive`.

Worker :

- healthcheck de vie minimal sur le processus du conteneur.
- il n'expose pas d'endpoint HTTP à ce stade.

Si le reverse proxy expose publiquement `/health` ou `/alive`, restreindre l'accès côté proxy si nécessaire.

## Stratégie de migrations

Choix retenu pour ce dépôt : migration au démarrage contrôlée par configuration.

La valeur par défaut production reste :

```env
PRIVATE_LISTINGS_APPLY_MIGRATIONS_ON_STARTUP=false
```

Pour une première installation ou une livraison contenant des migrations EF Core :

1. Sauvegarder la base si elle existe.
2. Planifier une fenêtre de déploiement.
3. Passer temporairement `PRIVATE_LISTINGS_APPLY_MIGRATIONS_ON_STARTUP=true` dans `/opt/privatelistings/.env`.
4. Déployer et vérifier les logs de `privatelistings-web`.
5. Repasser `PRIVATE_LISTINGS_APPLY_MIGRATIONS_ON_STARTUP=false`.
6. Redéployer pour revenir au mode nominal.

Le worker ne lance pas les migrations. Le point de contrôle est donc le démarrage du service web.

Si les migrations deviennent risquées ou longues, il faudra ajouter un job de migration dédié avant de garder cette stratégie en production.

## Déploiement manuel local sur le GEEKOM

Depuis `/opt/privatelistings` :

```bash
docker compose --env-file .env -f docker-compose.geekom.yml pull
docker compose --env-file .env -f docker-compose.geekom.yml up -d --remove-orphans
docker compose --env-file .env -f docker-compose.geekom.yml ps
```

Purge optionnelle des anciennes images :

```bash
docker image prune -f
```

Avec PowerShell depuis le dossier du dépôt, pour cibler un GEEKOM déjà préparé :

```powershell
.\deploy\scripts\deploy.ps1 -RemoteHost "<host>" -RemoteUser "<user>" -RemotePath "/opt/privatelistings" -ImageTag "latest"
```

Pour un déploiement depuis le GEEKOM lui-même :

```powershell
.\deploy\scripts\deploy.ps1 -ComposeFile ".\docker-compose.geekom.yml" -EnvFile ".\.env"
```

## Workflow GitHub Actions

Le workflow `.github/workflows/deploy.yml` est déclenché manuellement avec `workflow_dispatch`.

Modes :

- `push-only` : construit et publie les images GHCR.
- `ssh-geekom` : construit, publie, puis exécute sur le GEEKOM :
  - `docker compose pull`
  - `docker compose up -d --remove-orphans`
  - `docker image prune -f` si l'option est cochée.

Secrets GitHub à prévoir :

- `GITHUB_TOKEN` : disponible automatiquement pour publier sur GHCR si les permissions du dépôt l'autorisent.
- `GHCR_TOKEN` : optionnel, à utiliser si un jeton dédié est nécessaire.
- `GEEKOM_HOST` : hôte SSH.
- `GEEKOM_USER` : utilisateur SSH.
- `GEEKOM_SSH_KEY` : clé privée SSH.
- `GEEKOM_SSH_KNOWN_HOSTS` : optionnel mais recommandé pour éviter de dépendre de `ssh-keyscan`.

Les variables de production et secrets applicatifs restent dans `/opt/privatelistings/.env` ou dans le mécanisme de secrets choisi côté GEEKOM. Ils ne sont pas écrits dans le workflow.

## Option A : Watchtower

Si Watchtower est déjà installé sur le GEEKOM, le compose pose le label :

```yaml
com.centurylinklabs.watchtower.enable: "true"
```

Activer dans `.env` :

```env
WATCHTOWER_ENABLE=true
```

Watchtower doit être lancé avec `--label-enable` pour ne surveiller que les conteneurs explicitement autorisés. Dans ce mode, le workflow peut rester en `push-only` : le GEEKOM mettra à jour les conteneurs après publication des images.

Points de vigilance :

- planifier une fenêtre de mise à jour ;
- sauvegarder avant les versions contenant des migrations ;
- ne pas activer les migrations automatiques permanentes sans accepter le risque opérationnel.

## Option B : SSH depuis GitHub Actions

Le mode `ssh-geekom` est préférable si l'on veut déclencher explicitement le déploiement après publication des images.

Préconditions :

- `/opt/privatelistings/docker-compose.geekom.yml` existe sur le GEEKOM ;
- `/opt/privatelistings/.env` existe sur le GEEKOM ;
- l'utilisateur SSH peut exécuter `docker compose` ;
- le GEEKOM peut lire les images GHCR publiées.

Si les packages GHCR sont privés, connecter le GEEKOM à GHCR :

```bash
echo "<token-ghcr>" | docker login ghcr.io -u "<owner>" --password-stdin
```

## Sauvegarde PostgreSQL

Depuis le dépôt ou depuis le dossier de déploiement, avec PowerShell :

```powershell
.\deploy\scripts\backup-db.ps1 -EnvFile ".\deploy\.env" -ComposeFile ".\deploy\docker-compose.geekom.yml"
```

Sur le GEEKOM depuis `/opt/privatelistings`, adapter les chemins :

```powershell
.\scripts\backup-db.ps1 -EnvFile ".\.env" -ComposeFile ".\docker-compose.geekom.yml"
```

Le script :

- exécute `pg_dump` dans `privatelistings-postgres` ;
- produit un fichier `privatelistings-db-YYYYMMDD-HHMMSS.dump` ;
- applique la rétention configurée par `PRIVATE_LISTINGS_BACKUP_RETENTION_DAYS`.

Les sauvegardes doivent être protégées comme des données de production : accès restreint, chiffrement si possible, copie hors machine et test périodique de restauration.

## Restauration PostgreSQL

Par défaut, le script stoppe `privatelistings-web` et `privatelistings-worker`, restaure la base, puis relance les services applicatifs.

```powershell
.\deploy\scripts\restore-db.ps1 -BackupPath ".\deploy\backups\privatelistings-db-YYYYMMDD-HHMMSS.dump" -EnvFile ".\deploy\.env" -ComposeFile ".\deploy\docker-compose.geekom.yml"
```

Pour recréer la base avant restauration :

```powershell
.\deploy\scripts\restore-db.ps1 -BackupPath ".\deploy\backups\privatelistings-db-YYYYMMDD-HHMMSS.dump" -RecreateDatabase -Force
```

Ne restaurer en production qu'après avoir identifié clairement la sauvegarde cible et l'impact sur les données créées depuis cette sauvegarde.

## Gestion des uploads

Les uploads ne sont pas dans l'image Docker. Ils vivent dans le volume `privatelistings_uploads`.

À sauvegarder avec la base :

```bash
docker run --rm \
  -v privatelistings_uploads:/data:ro \
  -v "$PWD/backups:/backups" \
  alpine \
  tar -czf /backups/privatelistings-uploads-$(date +%Y%m%d-%H%M%S).tar.gz -C /data .
```

Restaurer les uploads uniquement après arrêt des services applicatifs pour éviter un état incohérent entre base et fichiers.

## Logs

Les services écrivent sur stdout/stderr. Le compose configure le driver `json-file` avec rotation :

```env
DOCKER_LOG_MAX_SIZE=10m
DOCKER_LOG_MAX_FILE=5
```

Consultation :

```bash
docker compose --env-file .env -f docker-compose.geekom.yml logs -f privatelistings-web
docker compose --env-file .env -f docker-compose.geekom.yml logs -f privatelistings-worker
```

Purge des logs : Docker supprime les anciens fichiers selon la rotation configurée. Aucune écriture de log applicatif sur volume dédié n'est prévue à ce stade.

## Mode Demo séparé

Le mode `Demo` doit rester isolé de la production.

Utiliser au minimum :

```env
ASPNETCORE_ENVIRONMENT=Demo
POSTGRES_DB=privatelistings_demo
PRIVATE_LISTINGS_POSTGRES_VOLUME=privatelistings_demo_postgres_data
PRIVATE_LISTINGS_UPLOADS_VOLUME=privatelistings_demo_uploads
PRIVATE_LISTINGS_DEMO_SEED_ENABLED=true
PRIVATE_LISTINGS_SMTP_HOST=privatelistings-mailpit
PRIVATE_LISTINGS_SMTP_PORT=1025
```

Démarrage avec MailPit :

```bash
docker compose --env-file .env -f docker-compose.geekom.yml --profile nonproduction up -d
```

Ne jamais partager les volumes, la base ou les uploads entre `Demo` et `Production`.
