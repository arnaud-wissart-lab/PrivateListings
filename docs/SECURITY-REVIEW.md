# Revue sécurité passive

Date de revue : 2026-07-07

Cette revue est passive : lecture du code, des configurations, des Dockerfiles, du Docker Compose, des workflows GitHub Actions et des documents `SECURITY`, `PRIVACY` et `DEPLOYMENT`. Aucun scan externe agressif, brute force réel, fuzzing lourd ni test offensif sur domaine public n'a été exécuté.

## Synthèse

Le socle de sécurité est déjà sérieux pour un MVP : ASP.NET Core Identity, cookies sécurisés, activation et reset password par liens temporaires à usage unique, rate limiting applicatif, validations d'uploads par extension, MIME et signature, stockage des images hors `wwwroot`, protections serveur sur les propriétaires d'annonces, participants de conversations et rôles back-office, et audit log sur plusieurs actions sensibles.

Les corrections appliquées pendant cette revue portent sur les points simples et non controversés :

- accès visiteur aux annonces et images d'annonces fermé ;
- headers HTTP de base ajoutés ;
- logout protégé explicitement par antiforgery et réponse `400` en cas de jeton manquant ;
- `returnUrl` externe ou protocol-relative ignorée après login ;
- limite multipart alignée avec la limite d'upload image ;
- clé interne du rate limiter annonces hachée ;
- seed de démonstration interdit en production sans exception configurable.

Les risques principaux restant ouverts avant production sont : base URL publique et filtrage `Host`, changement d'email sans confirmation de la nouvelle adresse, MFA administrateur non appliquée, persistance des clés Data Protection en Docker, CSP, suppression/anonymisation effective, purge des jetons expirés, sauvegardes chiffrées et testées, et durcissement Docker.

## Risques

### PL-SEC-001

- Gravité : High
- Zone concernée : Annonces, autorisation, données personnelles
- Fichiers concernés : `src/PrivateListings.Web/Components/Pages/Listings.razor`, `src/PrivateListings.Web/Components/Pages/ListingDetails.razor`, `src/PrivateListings.Web/Listings/ListingEndpointRouteBuilderExtensions.cs`, `src/PrivateListings.Web/Components/Layout/*`, `src/PrivateListings.Web/Components/Pages/Home.razor`
- Description : les visiteurs non authentifiés pouvaient consulter la liste des annonces publiées, ouvrir le détail d'une annonce publiée et accéder aux images publiées par URL directe.
- Impact : exposition de contenus d'annonces dans un produit documenté comme privé ou semi-privé.
- Recommandation : exiger l'authentification sur les pages d'annonces, les détails et l'endpoint d'images ; masquer les liens d'annonces dans la navigation visiteur.
- Statut : Fixed
- Tests ajoutés si correction : `SecurityE2ETests.VisitorCannotOpenListings`, `SecurityE2ETests.VisitorCannotOpenListingImages`, adaptation de `VisitorPagesTests`.

### PL-SEC-002

- Gravité : Medium
- Zone concernée : Headers HTTP
- Fichiers concernés : `src/PrivateListings.Web/Program.cs`, `src/PrivateListings.Web/SecurityHeadersMiddlewareExtensions.cs`
- Description : seuls HSTS et HTTPS redirection étaient configurés ; les headers de base `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy` et `Permissions-Policy` étaient absents.
- Impact : surface accrue pour clickjacking, sniffing de contenu et exposition de référents.
- Recommandation : ajouter ces headers globalement sans introduire encore de CSP stricte.
- Statut : Fixed
- Tests ajoutés si correction : `SecurityE2ETests.PublicPageIncludesSecurityHeaders`.

### PL-SEC-003

- Gravité : Medium
- Zone concernée : Headers HTTP, XSS
- Fichiers concernés : `src/PrivateListings.Web/Components/App.razor`, `src/PrivateListings.Web/Program.cs`, `src/PrivateListings.Web/wwwroot/app.css`
- Description : aucune `Content-Security-Policy` n'est définie. Les sorties Razor encodent correctement les contenus utilisateur et aucun rendu HTML brut n'a été trouvé, mais CSP reste une barrière utile.
- Impact : en cas de XSS futur ou de dépendance front-end compromise, l'absence de CSP limite la défense en profondeur.
- Recommandation : définir une CSP compatible Blazor avec inventaire des scripts/styles requis, puis ajouter des tests de non-régression sur les headers.
- Statut : Open
- Tests ajoutés si correction : aucun, non corrigé.

### PL-SEC-004

- Gravité : High
- Zone concernée : Mode démo, configuration Production
- Fichiers concernés : `src/PrivateListings.Infrastructure/Persistence/Seeding/PrivateListingsDemoSeedPolicy.cs`, `src/PrivateListings.Infrastructure/Persistence/Seeding/PrivateListingsDemoSeedOptions.cs`, `deploy/docker-compose.geekom.yml`, `deploy/.env.example`, `docs/SECURITY.md`, `docs/DEPLOYMENT.md`
- Description : l'option `AllowInProduction` permettait théoriquement d'exécuter le seed de démonstration en production si elle était activée.
- Impact : risque d'injection de comptes, annonces et conversations fictives dans une base de production.
- Recommandation : interdire le seed démo en `Production` sans option de contournement et réserver les données démo aux environnements `Development` ou `Demo`.
- Statut : Fixed
- Tests ajoutés si correction : `PrivateListingsPersistenceTests.SeedDemoEstRefuseEnProductionQuandIlEstActive`.

### PL-SEC-005

- Gravité : Medium
- Zone concernée : CSRF, sessions
- Fichiers concernés : `src/PrivateListings.Web/Components/Account/IdentityComponentsEndpointRouteBuilderExtensions.cs`
- Description : le logout était un `POST` authentifié avec formulaire antiforgery côté UI, mais l'endpoint ne validait pas explicitement le jeton.
- Impact : selon l'évolution du binding minimal API et des métadonnées antiforgery, un logout forcé pouvait devenir possible.
- Recommandation : valider explicitement l'antiforgery dans l'endpoint et retourner `400` si le jeton est absent ou invalide.
- Statut : Fixed
- Tests ajoutés si correction : `SecurityE2ETests.LogoutRejectsMissingAntiforgeryToken`.

### PL-SEC-006

- Gravité : Medium
- Zone concernée : Authentification, redirections
- Fichiers concernés : `src/PrivateListings.Web/Components/Account/IdentityRedirectManager.cs`
- Description : la redirection post-login acceptait une valeur `returnUrl` sans filtrage local strict.
- Impact : risque d'open redirect ou de comportement ambigu avec des URL externes ou protocol-relative.
- Recommandation : n'autoriser que les URL locales relatives ou convertibles depuis la base applicative ; ignorer le reste.
- Statut : Fixed
- Tests ajoutés si correction : `SecurityE2ETests.LoginIgnoresExternalReturnUrl`.

### PL-SEC-007

- Gravité : Medium
- Zone concernée : Uploads
- Fichiers concernés : `src/PrivateListings.Web/Program.cs`, `src/PrivateListings.Web/Listings/ListingEndpointRouteBuilderExtensions.cs`, `src/PrivateListings.Infrastructure/Storage/FileSystemListingImageStorage.cs`
- Description : le service de stockage refusait les fichiers au-delà de `MaxSizeInBytes`, mais le plafond multipart ASP.NET Core restait plus élevé que la limite métier.
- Impact : consommation mémoire et bande passante inutile avant le rejet applicatif.
- Recommandation : configurer `FormOptions.MultipartBodyLengthLimit` à partir de la limite d'upload image avec une marge d'enveloppe multipart.
- Statut : Fixed
- Tests ajoutés si correction : `ListingImageStorageTests.SaveAsyncRefuseUneImageTropGrande`.

### PL-SEC-008

- Gravité : Low
- Zone concernée : Rate limiting, minimisation des données
- Fichiers concernés : `src/PrivateListings.Application/Listings/InMemoryListingRateLimiter.cs`
- Description : le rate limiter annonces conservait en mémoire une clé contenant l'identifiant acteur et l'adresse IP normalisés en clair.
- Impact : exposition inutile de données techniques en cas de dump mémoire ou diagnostic intrusif.
- Recommandation : hacher la clé interne comme pour les modules compte et messagerie.
- Statut : Fixed
- Tests ajoutés si correction : `ListingRateLimiterTests.CleInterneNeContientPasIdentifiantsBruts`, `ListingRateLimiterTests.TryAcquireAsyncBloqueApresLaLimite`.

### PL-SEC-009

- Gravité : High
- Zone concernée : Emails transactionnels, reset password, activation, messagerie
- Fichiers concernés : `src/PrivateListings.Web/Components/Account/Pages/Register.razor`, `src/PrivateListings.Web/Components/Account/Pages/ForgotPassword.razor`, `src/PrivateListings.Web/Messaging/MessagingEndpointRouteBuilderExtensions.cs`, `src/PrivateListings.Infrastructure/Identity/PrivateListingsIdentityEmailSender.cs`, `src/PrivateListings.Infrastructure/Messaging/SmtpMessageNotificationSender.cs`, `src/PrivateListings.Web/appsettings.json`
- Description : les liens d'activation, reset password et notifications de conversation sont construits à partir de l'URL ou du `Host` de la requête. `AllowedHosts` vaut `*`.
- Impact : si un `Host` non fiable atteint l'application ou le reverse proxy, un email de sécurité peut contenir un domaine contrôlé par un attaquant.
- Recommandation : ajouter une configuration `PrivateListings:PublicBaseUrl` obligatoire en production, générer les liens email depuis cette valeur, et restreindre `AllowedHosts` au domaine réel.
- Statut : Open
- Tests ajoutés si correction : aucun, non corrigé.

### PL-SEC-010

- Gravité : High
- Zone concernée : Gestion des rôles, administration
- Fichiers concernés : `src/PrivateListings.Infrastructure/BackOffice/EfBackOfficeService.cs`, `src/PrivateListings.Infrastructure/Identity/PrivateListingsUser.cs`, `src/PrivateListings.Web/Admin/BackOfficeAuthorizationOptionsExtensions.cs`
- Description : `MfaRequiredForAdmin` est renseigné pour les administrateurs, mais aucun parcours MFA ni policy serveur ne bloque l'accès admin si `TwoFactorEnabled` est faux.
- Impact : un compte administrateur compromis par mot de passe seul donne accès au back-office.
- Recommandation : finaliser le parcours MFA et exiger une claim ou policy MFA pour `BackOffice.Admin`.
- Statut : Open
- Tests ajoutés si correction : aucun, non corrigé.

### PL-SEC-011

- Gravité : High
- Zone concernée : Authentification, données personnelles
- Fichiers concernés : `src/PrivateListings.Infrastructure/Identity/IdentityUserAccountService.cs`, `src/PrivateListings.Web/Components/Account/Pages/AccountSecurity.razor`
- Description : le changement d'email exige le mot de passe actuel, mais marque immédiatement la nouvelle adresse comme confirmée sans lien de confirmation envoyé à cette adresse.
- Impact : erreur de saisie ou session compromise avec mot de passe connu peut détourner l'adresse de contact et les futurs emails de sécurité.
- Recommandation : envoyer un lien de confirmation à la nouvelle adresse, conserver l'ancienne jusqu'à confirmation ou marquer le compte non confirmé pendant la transition.
- Statut : Open
- Tests ajoutés si correction : aucun, non corrigé.

### PL-SEC-012

- Gravité : Medium
- Zone concernée : Cookies, tokens Identity, déploiement Docker
- Fichiers concernés : `src/PrivateListings.Web/Program.cs`, `deploy/docker-compose.geekom.yml`, `docs/DEPLOYMENT.md`
- Description : aucune persistance explicite du key ring ASP.NET Core Data Protection n'est configurée en production Docker.
- Impact : recréation de conteneur ou changement d'instance peut invalider cookies, liens d'activation et liens de reset encore valides.
- Recommandation : configurer un volume dédié aux clés Data Protection, une application name stable et une protection adaptée du volume.
- Statut : Open
- Tests ajoutés si correction : aucun, non corrigé.

### PL-SEC-013

- Gravité : Medium
- Zone concernée : Rate limiting
- Fichiers concernés : `src/PrivateListings.Application/Authentication/InMemoryAccountRateLimiter.cs`, `src/PrivateListings.Application/Listings/InMemoryListingRateLimiter.cs`, `src/PrivateListings.Application/Messaging/InMemoryMessagingRateLimiter.cs`
- Description : les limites sont en mémoire par processus.
- Impact : limites contournables après redémarrage et non partagées en multi-instance.
- Recommandation : conserver en MVP mono-instance ; avant multi-instance, basculer vers un stockage distribué ou une protection reverse proxy cohérente.
- Statut : Deferred
- Tests ajoutés si correction : non applicable.

### PL-SEC-014

- Gravité : Medium
- Zone concernée : Suppression/anonymisation, données personnelles
- Fichiers concernés : `src/PrivateListings.Infrastructure/Identity/IdentityUserAccountService.cs`, `src/PrivateListings.Domain/Users/UserProfile.cs`, `src/PrivateListings.Web/Components/Account/Pages/AccountSecurity.razor`, `docs/PRIVACY.md`
- Description : la demande de suppression de compte est auditée, mais aucun workflow d'anonymisation ou suppression effective n'est implémenté côté application.
- Impact : non-conformité fonctionnelle et confidentialité incomplète avant production.
- Recommandation : implémenter un cas d'utilisation de suppression/anonymisation, définir les règles sur annonces, messages, images, audit logs et exports, puis tester ces règles.
- Statut : Open
- Tests ajoutés si correction : aucun, non corrigé.

### PL-SEC-015

- Gravité : Low
- Zone concernée : Tokens temporaires, maintenance
- Fichiers concernés : `src/PrivateListings.Infrastructure/Identity/AccountSecurityTokenStore.cs`, `src/PrivateListings.Worker/Worker.cs`
- Description : les jetons expirés sont refusés à l'usage, mais aucune purge périodique des entrées expirées n'est implémentée.
- Impact : accumulation de données techniques inutiles et charge progressive de la table `account_security_tokens`.
- Recommandation : ajouter une tâche worker de purge avec rétention courte, audit minimal et tests d'intégration.
- Statut : Open
- Tests ajoutés si correction : aucun, non corrigé.

### PL-SEC-016

- Gravité : Medium
- Zone concernée : Audit logs
- Fichiers concernés : `src/PrivateListings.Infrastructure/Identity/IdentityUserAccountService.cs`, `src/PrivateListings.Infrastructure/BackOffice/EfBackOfficeService.cs`, `src/PrivateListings.Domain/Auditing/AuditLog.cs`
- Description : les actions sensibles sont auditées, mais les entrées ne portent pas systématiquement le résultat, le motif structuré et l'identifiant de corrélation attendus par `docs/SECURITY.md`.
- Impact : investigation incident plus difficile et corrélation incomplète entre logs techniques et audit métier.
- Recommandation : enrichir le modèle ou les métadonnées d'audit avec `result`, `reasonCode` lorsque pertinent et `correlationId`, sans stocker de secret.
- Statut : Open
- Tests ajoutés si correction : aucun, non corrigé.

### PL-SEC-017

- Gravité : Low
- Zone concernée : Health checks, configuration Production
- Fichiers concernés : `src/PrivateListings.ServiceDefaults/Extensions.cs`, `deploy/docker-compose.geekom.yml`, `docs/DEPLOYMENT.md`
- Description : les endpoints `/health` et `/alive` sont exposés si `PrivateListings:HealthChecks:ExposeEndpoints=true`, valeur utilisée par le compose pour les healthchecks.
- Impact : exposition d'informations de disponibilité si le reverse proxy publie ces routes sans restriction.
- Recommandation : garder l'accès limité au réseau local ou au reverse proxy, ou séparer un endpoint interne de healthcheck Docker d'un endpoint public restreint.
- Statut : Deferred
- Tests ajoutés si correction : non applicable.

### PL-SEC-018

- Gravité : Medium
- Zone concernée : Sauvegardes/restauration
- Fichiers concernés : `deploy/scripts/backup-db.ps1`, `deploy/scripts/restore-db.ps1`, `docs/DEPLOYMENT.md`, `docs/PRIVACY.md`
- Description : les scripts de sauvegarde et restauration existent, mais le chiffrement, la copie hors machine, le contrôle d'accès et les tests de restauration périodiques ne sont pas automatisés.
- Impact : sauvegardes potentiellement aussi sensibles que la base production, avec risque de fuite ou de restauration non fiable.
- Recommandation : documenter et automatiser chiffrement, rétention, contrôle d'accès, sauvegarde des uploads et test régulier de restauration.
- Statut : Open
- Tests ajoutés si correction : aucun, non corrigé.

### PL-SEC-019

- Gravité : Medium
- Zone concernée : Déploiement Docker
- Fichiers concernés : `src/PrivateListings.Web/Dockerfile`, `src/PrivateListings.Worker/Dockerfile`, `deploy/docker-compose.geekom.yml`
- Description : les conteneurs applicatifs utilisent un utilisateur non-root, mais le compose ne configure pas encore `read_only`, `cap_drop`, `security_opt: no-new-privileges` ni limites de ressources.
- Impact : durcissement conteneur incomplet en cas de compromission applicative.
- Recommandation : ajouter progressivement ces protections, en conservant les volumes nécessaires pour uploads et données techniques.
- Statut : Deferred
- Tests ajoutés si correction : non applicable.

### PL-SEC-020

- Gravité : Info
- Zone concernée : Secrets
- Fichiers concernés : dépôt complet, `.github/workflows/*.yml`, `deploy/.env.example`, `src/**/appsettings*.json`
- Description : aucun secret réel évident n'a été trouvé par recherche passive. Les mots de passe présents sont des valeurs de test ou de démonstration sur `example.test`, et `deploy/.env.example` utilise des placeholders.
- Impact : pas d'impact immédiat identifié.
- Recommandation : activer un secret scanning CI dédié avant production et conserver les vrais `.env` hors Git.
- Statut : Accepted
- Tests ajoutés si correction : non applicable.

### PL-SEC-021

- Gravité : Info
- Zone concernée : XSS, contenus utilisateur
- Fichiers concernés : `src/PrivateListings.Web/Components/**/*.razor`, `src/PrivateListings.Domain/Common/Guard.cs`
- Description : aucune utilisation de `MarkupString`, `Html.Raw` ou rendu HTML utilisateur brut n'a été trouvée. Les champs utilisateur sont rendus via Razor et donc encodés.
- Impact : le risque XSS applicatif direct est réduit, même si les entrées HTML sont stockées comme texte.
- Recommandation : garder cette convention, ajouter des tests de non-rendu HTML brut lors de l'introduction de contenu riche, et refuser explicitement le HTML si une API JSON publique apparaît.
- Statut : Accepted
- Tests ajoutés si correction : non applicable.

### PL-SEC-022

- Gravité : Info
- Zone concernée : Uploads
- Fichiers concernés : `src/PrivateListings.Infrastructure/Storage/FileSystemListingImageStorage.cs`, `tests/PrivateListings.IntegrationTests/ListingModuleIntegrationTests.cs`
- Description : les SVG sont explicitement refusés, les extensions autorisées sont JPEG, PNG et WebP, le type MIME est comparé au type attendu, la signature réelle est vérifiée et les métadonnées connues sont supprimées opportunistiquement.
- Impact : contrôle conforme au MVP.
- Recommandation : ajouter plus tard recompression, validation de dimensions et analyse antivirus si les usages ou volumes augmentent.
- Statut : Accepted
- Tests ajoutés si correction : tests existants `SaveAsyncRefuseLesSvg`, `SaveAsyncRefuseUnTypeInvalide`, `InvalidUploadIsRejected`.

## Contrôles minimum

| Contrôle | Résultat |
| --- | --- |
| Aucun secret réel dans le repo | Conforme à la revue passive ; secret scanning dédié encore recommandé. |
| Pas de mot de passe envoyé par email | Conforme ; les emails d'activation/reset ne contiennent pas le mot de passe. |
| Tokens reset/activation expirants et usage unique | Conforme ; durée 2 h, hachage en base, consommation unique, tests existants. |
| Rate limiting endpoints sensibles | Partiellement conforme ; comptes, annonces et messagerie couverts, stockage en mémoire seulement. |
| Uploads validés strictement | Conforme MVP ; limite multipart ajoutée, SVG refusé. |
| Pas de HTML libre utilisateur rendu | Conforme à la lecture du code. |
| Autorisations serveur testées | Partiellement conforme ; back-office, conversations et annonces ont des tests, accès visiteur annonces ajouté. |
| Cookies configurés correctement | Conforme pour cookie auth et status cookie ; Data Protection production reste ouverte. |
| Headers sécurité documentés | Partiellement conforme ; headers de base corrigés, CSP ouverte. |
| Messages privés non exposés dans emails/logs | Conforme à la lecture ; tests existants sur absence de corps de message dans emails. |
| Actions admin auditées | Partiellement conforme ; audit présent, corrélation/résultat à compléter. |
| Mode Demo isolé | Renforcé ; seed production impossible après correction. |
| Suppression/anonymisation documentée | Documentée mais pas encore implémentée. |
| Déploiement sans secrets commités | Conforme ; secrets attendus via `.env` non versionné ou GitHub Secrets. |

## Risques restants prioritaires

1. PL-SEC-009 : définir une base URL publique fiable et restreindre `AllowedHosts`.
2. PL-SEC-011 : confirmer la nouvelle adresse lors d'un changement d'email.
3. PL-SEC-010 : imposer MFA aux administrateurs.
4. PL-SEC-012 : persister les clés Data Protection en Docker.
5. PL-SEC-003 : ajouter une CSP compatible Blazor.
6. PL-SEC-014 : implémenter suppression/anonymisation.
7. PL-SEC-018 : chiffrer, externaliser et tester les sauvegardes.
