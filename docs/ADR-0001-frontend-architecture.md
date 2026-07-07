# ADR-0001 - Architecture frontend

Date : 2026-07-07
Statut : proposé

## Contexte

PrivateListings est une plateforme responsive desktop/mobile d'annonces privées, de services, d'entraide locale, d'événements, de profils, de messagerie et de back-office. Le backend cible reste .NET 10, avec architecture propre `Domain` / `Application` / `Infrastructure`, PostgreSQL, Docker Desktop en développement, Docker Compose en production sur mini-PC GEEKOM, GitHub Actions et tests automatisés.

Le dépôt contient déjà un squelette .NET 10 :

- `src/PrivateListings.Web` : application ASP.NET Core / Blazor Web App avec composants interactifs serveur.
- `src/PrivateListings.AppHost` : orchestration Aspire de `web`, `worker`, PostgreSQL et MailPit.
- `src/PrivateListings.Application`, `src/PrivateListings.Domain`, `src/PrivateListings.Infrastructure`.
- `src/PrivateListings.Worker`.
- tests de domaine, application et intégration.

Avant de poursuivre les pages, l'authentification et l'UI, il faut confirmer si le frontend doit rester Blazor ou basculer vers une pile JavaScript.

L'inspection locale de `E:\Assets\metronic-v9.5.0` montre que Metronic 9.5.0 est principalement orienté Tailwind, HTML, React Vite et React Next.js. Aucun paquet Angular applicatif ni fichier `angular.json` n'a été trouvé. Le paquet Bootstrap complet n'est pas présent localement ; seul `download_full_v8_bootstrap_package.txt` indique que Metronic v8 Bootstrap est séparé.

Références vérifiées :

- Source locale Metronic : `E:\Assets\metronic-v9.5.0`.
- Documentation officielle Blazor sécurité : https://learn.microsoft.com/en-us/aspnet/core/blazor/security/?view=aspnetcore-10.0
- Documentation officielle CSRF ASP.NET Core : https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0
- Documentation officielle Aspire pour applications JavaScript : https://learn.microsoft.com/en-us/dotnet/aspire/get-started/build-aspire-apps-with-nodejs
- Documentation officielle Next.js self-hosting : https://nextjs.org/docs/app/guides/self-hosting
- Documentation officielle Angular SSR et hydratation : https://angular.dev/guide/ssr et https://angular.dev/guide/hydration

### Inventaire Metronic local

| Paquet local | Variante observée | Contenu utile | Lecture pour PrivateListings |
|---|---|---|---|
| `metronic-tailwind-html-demos` | HTML + Tailwind + KTUI | `demo1` à `demo10`, 1 174 pages HTML au total, assets `css`, `js`, `media`, `vendors` | Source principale si Blazor est conservé, car les classes et structures HTML peuvent être adaptées en Razor sans React |
| `metronic-tailwind-html-starter-kit` | HTML + Tailwind + KTUI | `layout-1` à `layout-17`, plus `layout-1/dark-sidebar` | Bonne base de layouts légers, surtout `layout-3`, `layout-11`, `layout-12` |
| `metronic-tailwind-react-demos` | React Vite et React Next.js, JS et TS | Démos React complètes, routes auth/protected, user management, account, profils, store | Très riche si le frontend devient React, mais lourd à découpler du modèle Metronic |
| `metronic-tailwind-react-starter-kit` | React Vite et React Next.js, JS et TS | Starter React, nombreux layouts et composants UI | Utile pour accélérer un frontend React, mais ajoute une surface npm importante |
| `metronic-tailwind-react-concepts` | Concepts React Vite et React Next.js, JS et TS | `ai`, `api`, `calendar`, `crm`, `mail`, `real-estate`, `store-inventory`, `todo` | Très bon catalogue UX, surtout `mail` et `real-estate` |
| `metronic-tailwind-nextjs-landings` | Next.js landing SaaS | Projet `saas` | Peu pertinent pour l'application connectée ; plutôt marketing |
| `figma` | Fichier design | `Metronic_v9.5.0.fig` | Référence visuelle uniquement |
| `download_full_v8_bootstrap_package.txt` | Bootstrap séparé | Lien vers Metronic v8 Bootstrap | Bootstrap n'est pas disponible localement et ne doit pas être introduit pour le MVP |

Les assets HTML distribués contiennent notamment :

- `assets/css` : 2 fichiers, environ 0,52 Mo.
- `assets/js` : 12 fichiers, environ 1,60 Mo.
- `assets/media` : 682 fichiers, environ 20,62 Mo.
- `assets/vendors` : 298 fichiers, environ 19,20 Mo.

Vendors observés : `@form-validation`, `apexcharts`, `canvas-confetti`, `clipboard`, `datatables-net`, `dropzone`, `fullcalendar`, `jquery`, `keenicons`, `ktui`, `leaflet`, `prismjs`, `tinymce`.

Ces vendors ne doivent pas être importés globalement. Pour le MVP, la cible raisonnable est Tailwind, Keenicons ou une alternative cohérente, et éventuellement KTUI de façon temporaire et limitée.

### Templates Metronic les plus pertinents

| Besoin produit | Templates locaux les plus complets | Verdict |
|---|---|---|
| Liste d'annonces | `metronic-tailwind-html-demos/dist/html/demo1/store-client/search-results-grid.html`, `search-results-list.html`, composants React `card-project-row.tsx`, concept `real-estate/pages/card.tsx` | Très exploitable. Reprendre grille, liste, filtres, badges et cartes ; supprimer panier, wishlist, achat et logique e-commerce |
| Détail annonce | `demo1/store-client/product-details.html`, concept `real-estate/page.tsx`, `real-estate/pages/card.tsx` | Bon point de départ pour galerie, résumé, caractéristiques et CTA de contact ; à réécrire avec le vocabulaire annonces |
| Profil utilisateur | `demo1/public-profile/profiles/plain.html`, `demo1/public-profile/profiles/default.html`, `demo1/account/home/user-profile.html` | Très pertinent pour profil public membre et compte ; supprimer métriques SaaS/B2B |
| Messagerie | `metronic-tailwind-react-concepts/typescript/nextjs/app/mail`, `metronic-tailwind-react-concepts/typescript/vite/src/mail` | Meilleur modèle UX disponible, mais uniquement React ; à réécrire si Blazor est conservé |
| User management | `demo1/account/members/team-members.html`, `team-members-datatable.html`, `demo1/user-table/app-roster.html`, routes React Next.js `app/(protected)/user-management/users` | Très complet. Garder les tables, rôles et statuts ; écarter DataTables/jQuery |
| Account settings | `demo1/account/home/settings-sidebar.html`, `settings-plain.html`, équivalents React `account/home/settings-enterprise` | Bonne structure de paramètres ; réduire fortement les champs et widgets |
| Security overview | `demo1/account/security/overview.html`, `security-log.html`, équivalents React `account/security/overview` et `security-log` | Très pertinent pour 2FA, sessions, appareils, logs et audit |
| Admin tables | `demo1/account/members/team-members.html`, `demo1/network/user-table/*.html`, `demo1/user-table/app-roster.html`, React `user-management/users/components/user-list.tsx` | Pertinent pour le back-office ; préférer table native côté framework plutôt que DataTables |
| Responsive mobile | Démos HTML avec drawers, starter `layout-11`, `layout-12`, concepts React `mail` et `real-estate` | Les patterns existent, mais l'expérience mobile PrivateListings doit être simplifiée et orientée annonces |

## Décision à prendre

Choisir l'architecture frontend à utiliser pour le MVP et poser ses conséquences sur :

- l'exploitation de Metronic 9.5.0 ;
- l'architecture .NET existante ;
- l'authentification et la sécurité ;
- l'orchestration Aspire ;
- la CI/CD et Docker Compose ;
- la vitesse de livraison ;
- la dette technique et la surface npm.

Options étudiées :

1. Blazor Web App.
2. React / Next.js.
3. React Vite SPA.
4. Angular.
5. ASP.NET Core API + frontend séparé.

## Options étudiées

### Option 1 - Blazor Web App

PrivateListings conserve `src/PrivateListings.Web` comme application web principale. Les pages publiques peuvent utiliser le rendu serveur statique ou SSR interactif selon le besoin. Les zones nécessitant de l'interaction, comme messagerie, filtres avancés, publication d'annonce ou back-office, utilisent des composants Razor.

Metronic est utilisé comme source HTML/Tailwind et catalogue de motifs visuels. Les pages React sont consultées comme références UX, mais ne sont pas importées comme dépendances runtime.

### Option 2 - React / Next.js

Le frontend devient une application Next.js séparée, probablement dans `apps/web`. Metronic React Next.js peut être repris plus directement. Le backend .NET devient une API ou un BFF. Next.js peut être déployé en image Docker Node avec sortie `standalone`, ou en export statique si les fonctionnalités le permettent.

Cette option maximise la réutilisation des sources React/Next Metronic, mais introduit une seconde application runtime, une surface npm importante et une conception d'authentification plus délicate.

### Option 3 - React Vite SPA

Le frontend devient une SPA React Vite séparée, servie statiquement par Nginx, Caddy ou ASP.NET Core. Metronic React Vite est réutilisable. Le backend .NET expose une API HTTP. Cette option est plus simple que Next.js côté runtime, mais perd les avantages SSR et rend l'authentification par cookies plus sensible aux choix de domaine, CSRF, CORS et reverse proxy.

### Option 4 - Angular

Le frontend devient une application Angular séparée. Angular est solide pour une application structurée, testable et maintenable, mais aucun template Angular Metronic 9.5.0 n'est présent localement. Il faudrait adapter les sources HTML/Tailwind à Angular sans bénéficier des composants React disponibles.

### Option 5 - ASP.NET Core API + frontend séparé

Cette option est un modèle d'architecture plutôt qu'un framework unique. Elle peut être combinée avec Next.js, React Vite ou Angular. Elle crée une frontière HTTP explicite entre `PrivateListings.Api` et un frontend séparé.

Elle devient pertinente si une équipe frontend dédiée apparaît, si une API publique/mobile est planifiée rapidement, ou si les besoins UI dépassent clairement le confort Blazor. Pour le MVP actuel, elle augmente le nombre de projets, de tests, d'images Docker, de dépendances et de points de sécurité.

## Analyse comparative

### Synthèse par critères

| Critère | Blazor Web App | React / Next.js | React Vite SPA | Angular | API + frontend séparé |
|---|---|---|---|---|---|
| Ergonomie Metronic | Bonne avec HTML/Tailwind, faible réutilisation directe des concepts React | Excellente, surtout avec React Next.js et concepts `mail` / `real-estate` | Très bonne avec React Vite | Faible à moyenne, aucun paquet Angular local | Dépend du frontend choisi |
| Complexité d'intégration | Faible à moyenne, car l'existant est déjà Blazor | Élevée : API/BFF, Node, routing, auth, reverse proxy | Moyenne à élevée : API, SPA, CORS/CSRF | Élevée : nouveau framework + adaptation Metronic | Élevée par nature |
| Performance perçue mobile | Bonne si SSR statique et CSS maîtrisé ; vigilance sur Interactive Server | Très bonne avec SSR/streaming bien conçu | Correcte après chargement, moins favorable au premier rendu | Bonne avec SSR/hydratation, mais plus lourd à mettre en place | Dépend du frontend et du découpage réseau |
| Charge serveur | Modérée à élevée si beaucoup d'interactivité serveur ; maîtrisable par SSR statique | Node SSR + API .NET, donc deux charges applicatives | API .NET surtout ; frontend statique | Node SSR optionnel + API .NET | API + frontend séparé |
| Charge client | Faible à moyenne ; peu de npm si discipline stricte | Moyenne à élevée avec React, hydratation et dépendances Metronic | Élevée côté SPA | Moyenne à élevée | Dépend du frontend |
| Auth sécurisée | Très favorable : cookies ASP.NET Core, antiforgery, peu de CORS | Faisable mais plus complexe : cookie same-origin, BFF ou proxy, CSRF | Plus délicat : éviter JWT en navigateur, privilégier BFF/proxy | Même complexité qu'une SPA séparée | Complexité centrale de l'option |
| CI/CD | Simple : restore/build/test .NET + build CSS limité | Plus complexe : .NET + Node + tests frontend + image Next | Plus complexe : .NET + Node + build statique | Plus complexe : .NET + Angular CLI | Plus complexe : pipelines multi-apps |
| Docker Compose | Simple : image web + worker + PostgreSQL + reverse proxy éventuel | Plus lourd : API/BFF + Next + worker + PostgreSQL + reverse proxy | Plus lourd : API + frontend statique + worker + PostgreSQL | Plus lourd : API + frontend + worker + PostgreSQL | Plus lourd par définition |
| Maintenabilité développeur .NET | Très favorable | Moyenne, demande expertise Next.js | Moyenne, demande expertise React SPA | Moyenne à faible si Angular n'est pas déjà maîtrisé | Moyenne, multiplie les frontières |
| Rapidité MVP | Très bonne pour auth, back-office, formulaires, domaine existant | Bonne pour UI Metronic, moins bonne pour backend/auth/API | Moyenne | Faible à moyenne | Faible à moyenne |
| Dette technique | Faible si Metronic est adapté proprement | Risque de dette npm et duplication de règles côté API/frontend | Risque de dette SPA et API prématurée | Risque d'adaptation coûteuse | Risque de surarchitecture MVP |
| Surface d'attaque npm | Faible à moyenne : Tailwind/KTUI ciblés | Élevée : environ 43 à 53 dépendances runtime selon paquet TS inspecté | Élevée : environ 47 à 53 dépendances runtime selon paquet TS inspecté | Non mesurée localement, mais nouvelle chaîne npm complète | Dépend du frontend, généralement plus élevée que Blazor |

### Lecture par option

Blazor Web App est le meilleur alignement avec le dépôt existant, les compétences .NET, l'authentification par cookies, l'antiforgery, Aspire et Docker Compose. Il réduit les frontières techniques et permet de livrer vite un MVP sécurisé. Son principal coût est de réécrire les meilleurs concepts React Metronic en Razor, notamment `mail` et `real-estate`.

Next.js est l'option la plus séduisante visuellement parce que Metronic fournit beaucoup de React prêt à l'emploi. Elle devient très intéressante si l'objectif principal devient une expérience frontend très riche, si une expertise React/Next est disponible, ou si l'on accepte une architecture séparée dès maintenant. En contrepartie, elle complique l'authentification, ajoute un runtime Node et augmente fortement la surface npm.

React Vite SPA est plus simple à héberger que Next.js, mais moins adaptée aux pages publiques d'annonces où le premier rendu, la découverte et la perception mobile comptent. Elle impose de concevoir une API complète plus tôt et augmente la complexité d'authentification sans apporter le SSR de Next.js.

Angular n'est pas recommandé ici. Le framework est robuste, mais Metronic 9.5.0 local ne fournit pas de variante Angular. Cette option cumule le coût d'un nouveau framework et le coût d'adaptation HTML/Tailwind, sans avantage fort pour le MVP.

ASP.NET Core API + frontend séparé est une bonne cible possible si le produit doit exposer rapidement une API publique ou mobile, ou si le frontend devient un produit séparé. Pour le MVP, c'est une complexité prématurée : plus de projets, plus de pipelines, plus de surfaces de sécurité, plus d'images Docker.

## Recommandation

Recommandation principale : conserver Blazor Web App pour le MVP.

Recommandation secondaire : si Blazor est abandonné après prototype ou arbitrage produit, choisir Next.js plutôt que React Vite ou Angular, avec backend .NET séparé et routage same-origin derrière reverse proxy.

Raisons de conserver Blazor maintenant :

- Le dépôt est déjà structuré autour de `PrivateListings.Web`, `AppHost`, `Worker`, PostgreSQL et MailPit.
- L'authentification par cookies ASP.NET Core, l'antiforgery, le rate limiting et les politiques d'autorisation s'intègrent plus directement.
- Les cas d'utilisation métier restent dans `Application`, sans dupliquer rapidement de logique côté frontend.
- Docker Compose reste simple pour un déploiement GEEKOM.
- La surface npm peut rester limitée à Tailwind, KTUI ciblé et éventuellement des assets d'icônes.
- Metronic HTML/Tailwind fournit assez de matière pour les pages MVP : annonces, détail, profils, compte, sécurité, tables admin.
- La messagerie et les pages très interactives peuvent être réalisées progressivement en Razor, puis renforcées avec SignalR ou endpoints dédiés si nécessaire.

Raisons de garder Next.js comme option secondaire :

- Les concepts Metronic `mail` et `real-estate` sont plus directement exploitables.
- Next.js offre un bon modèle SSR pour les pages publiques et peut être auto-hébergé en Docker.
- Si une expertise React/Next devient disponible, la vitesse de réalisation UI peut dépasser Blazor sur certaines surfaces.

Options non retenues pour le MVP :

- React Vite SPA : bon pour une application cliente interne, moins convaincant pour pages publiques, SEO, premier rendu mobile et auth cookie sans BFF.
- Angular : absence de paquet Metronic Angular local, coût d'adaptation trop élevé.
- API + frontend séparé dès maintenant : utile plus tard, mais prématuré pour le MVP.

## Conséquences

### Projets .NET

`PrivateListings.Web` doit être conservé comme application web principale. Il ne faut pas créer `PrivateListings.Api` maintenant.

La solution reste structurée ainsi :

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
```

`PrivateListings.Web` continue à référencer `Application`, `Infrastructure` et `ServiceDefaults`. La logique métier reste hors UI.

### Frontend séparé

Il ne faut pas créer `apps/web` ni `src/PrivateListings.Frontend` tant que Blazor est retenu.

Si la décision change plus tard :

- créer `src/PrivateListings.Api` pour l'API ASP.NET Core ;
- créer `apps/web` pour le frontend JavaScript, plutôt que `src/PrivateListings.Frontend`, afin de séparer clairement la convention monorepo JavaScript de la convention des projets .NET ;
- conserver `src/PrivateListings.Application`, `Domain`, `Infrastructure` et `Worker`.

### Authentification

Avec Blazor :

- `PrivateListings.Web` porte l'authentification par cookies ASP.NET Core ;
- utiliser cookies `Secure`, `HttpOnly`, `SameSite` ;
- conserver l'antiforgery sur les actions modifiant l'état ;
- gérer activation de compte et reset password par liens temporaires à usage unique ;
- appliquer le rate limiting côté ASP.NET Core ;
- garder les règles d'autorisation dans des policies et dans les cas d'utilisation applicatifs lorsque la règle est métier.

Si migration Next.js :

- éviter de stocker des JWT en navigateur pour le MVP ;
- privilégier une session cookie `HttpOnly` émise par ASP.NET Core ;
- servir frontend et API en same-origin derrière reverse proxy, par exemple `/` vers Next.js et `/api` vers `PrivateListings.Api` ;
- exposer un mécanisme CSRF explicite pour les mutations ;
- vérifier soigneusement les scénarios SSR qui doivent appeler l'API avec la session utilisateur.

### Metronic

Avec Blazor :

- utiliser `metronic-tailwind-html-demos` et `metronic-tailwind-html-starter-kit` comme références principales ;
- adapter les composants en Razor, ne pas coller des pages HTML complètes ;
- utiliser `metronic-tailwind-react-concepts` uniquement comme référence UX ;
- ne pas importer React, Next.js, Radix, DataTables ou jQuery ;
- exclure par défaut `apexcharts`, `fullcalendar`, `leaflet`, `tinymce`, `dropzone`, `canvas-confetti`, sauf besoin métier prouvé ;
- ne copier que les assets nécessaires, en respectant la licence Metronic.

### Aspire

Avec Blazor, l'`AppHost` existant reste pertinent :

```text
AppHost
  web -> PostgreSQL, MailPit
  worker -> PostgreSQL, MailPit
  postgres
  mailpit
```

Si frontend séparé :

- ajouter une application JavaScript dans l'`AppHost` via les intégrations Aspire JavaScript ;
- faire référencer le frontend à `PrivateListings.Api` ;
- ajouter éventuellement un reverse proxy local pour reproduire le same-origin de production.

### Docker

Avec Blazor pour le MVP :

- 2 images applicatives GHCR probables : `PrivateListings.Web` et `PrivateListings.Worker`.
- Services Compose : web, worker, PostgreSQL, volume PostgreSQL, volume fichiers privés, reverse proxy éventuel.
- MailPit reste un service de développement, pas un service de production.

Avec Next.js séparé :

- 3 images applicatives probables : `PrivateListings.Api`, `apps/web` Next.js, `PrivateListings.Worker`.
- Services Compose : frontend, API, worker, PostgreSQL, reverse proxy, volumes.

Avec React Vite SPA ou Angular séparé :

- 3 images applicatives probables si le frontend est servi par une image dédiée : API, frontend statique, worker.
- Variante possible : servir les fichiers statiques par ASP.NET Core, mais cela réduit l'intérêt d'une séparation stricte.

## Risques

### Risques de l'option Blazor

- Les concepts React Metronic les plus proches du métier, surtout `mail` et `real-estate`, devront être réécrits.
- Interactive Server peut être moins confortable sur mobile si la connexion est instable ou si trop de composants dépendent d'une connexion SignalR.
- Une adaptation trop littérale des pages HTML Metronic créerait un Razor lourd, fragile et difficile à maintenir.
- Metronic peut entraîner une surcharge CSS/JS si les assets sont copiés trop largement.

Mesures de réduction :

- utiliser SSR statique pour les pages publiques et limiter l'interactivité serveur aux surfaces qui en ont besoin ;
- créer des composants Razor propriétaires petits et testables ;
- fixer un budget CSS/JS avant intégration massive ;
- tester tôt mobile, latence réseau et tailles de bundle ;
- réécrire les tables, drawers, formulaires et filtres avec l'état Blazor plutôt qu'avec jQuery/DataTables.

### Risques des options React/Next/Vite

- Surface npm élevée et dépendances à surveiller dans Dependabot.
- Double pile .NET + Node à maintenir en CI/CD et Docker.
- Authentification plus complexe, surtout pour cookies, CSRF, SSR et appels API.
- Risque de duplication des validations entre frontend et backend.
- Plus grand nombre de points de panne en production sur mini-PC.

### Risques Angular

- Aucun support Metronic Angular local identifié.
- Coût d'adaptation élevé pour un bénéfice faible dans le contexte actuel.
- Dette de framework supplémentaire pour un développeur principalement .NET.

### Risques transverses

- La licence Metronic doit être validée avant toute distribution ou mise en production payante.
- Le dépôt ne doit pas intégrer par facilité les sources complètes Metronic, les démos complètes, le Figma ou les assets inutilisés.
- La décision doit être réévaluée si une application mobile native, une API publique ou une équipe frontend dédiée devient prioritaire.

## Plan d'implémentation

Plan recommandé en conservant Blazor :

1. Valider cette ADR comme décision frontend MVP.
2. Garder `PrivateListings.Web` comme application web principale.
3. Adapter le niveau d'interactivité Blazor : SSR statique par défaut pour pages publiques, composants interactifs seulement pour formulaires complexes, messagerie, publication et back-office.
4. Créer un prototype UI Blazor limité :
   - shell desktop inspiré de Demo 3 Compact Sidebar ;
   - navigation mobile dédiée ;
   - liste d'annonces inspirée de `store-client/search-results-grid.html` et `search-results-list.html` ;
   - détail annonce inspiré de `store-client/product-details.html` et `real-estate` ;
   - profil membre inspiré de `public-profile/profiles/plain.html` ;
   - sécurité compte inspirée de `account/security/overview.html` ;
   - admin utilisateurs inspiré de `account/members/team-members.html`, sans DataTables.
5. Mettre en place Tailwind côté `PrivateListings.Web`, avec scan des `.razor` et copie minimale des assets.
6. Mesurer le poids CSS/JS réel du prototype.
7. Ajouter les premiers tests UI/end-to-end sur parcours critiques : consultation annonce, login, publication, contact, admin utilisateurs.
8. Mettre à jour `docs/ARCHITECTURE.md`, `docs/SECURITY.md` et `docs/UI-DECISIONS.md` si cette ADR est acceptée.

Plan de migration si Blazor est abandonné :

1. Geler les changements UI Blazor et conserver uniquement les composants ou styles réutilisables comme référence.
2. Créer `src/PrivateListings.Api` avec ASP.NET Core, en réutilisant `Application`, `Domain` et `Infrastructure`.
3. Transformer `PrivateListings.Web` :
   - soit en projet supprimé de la trajectoire produit ;
   - soit en BFF minimal temporaire si nécessaire ;
   - soit en serveur d'assets si React Vite/Angular est choisi, mais ce n'est pas l'option préférée.
4. Créer `apps/web` :
   - Next.js si l'option secondaire est retenue ;
   - React Vite uniquement si SSR n'est pas nécessaire ;
   - Angular seulement si une contrainte forte l'impose.
5. Configurer le reverse proxy same-origin :
   - `/` vers le frontend ;
   - `/api` vers `PrivateListings.Api`.
6. Reconcevoir l'authentification :
   - cookies `HttpOnly` côté ASP.NET Core ;
   - endpoint CSRF ;
   - protections rate limiting ;
   - tests d'autorisation et de session.
7. Adapter `PrivateListings.AppHost` pour orchestrer API, frontend JavaScript, worker, PostgreSQL et MailPit.
8. Mettre à jour Docker Compose avec une image frontend supplémentaire.
9. Étendre la CI/CD à Node : install reproductible, lint, typecheck, tests frontend, build Docker.
10. Ajouter Dependabot pour npm, NuGet, GitHub Actions et images Docker.

## Décision proposée

Retenir Blazor Web App pour le MVP de PrivateListings.

L'option retenue conserve `PrivateListings.Web`, évite la création immédiate de `PrivateListings.Api` et d'un frontend séparé, limite la surface npm et s'aligne avec la sécurité ASP.NET Core, Aspire, Docker Compose et les compétences .NET. Metronic 9.5.0 doit être utilisé comme source HTML/Tailwind et catalogue UX, pas comme application copiée.

L'option secondaire est Next.js avec backend .NET séparé, seulement si un prototype Blazor révèle un coût d'adaptation trop élevé ou si l'équipe décide explicitement de privilégier une pile React. Dans ce cas, la migration doit passer par `src/PrivateListings.Api`, `apps/web`, reverse proxy same-origin, cookies `HttpOnly`, CSRF explicite, et une image Docker frontend supplémentaire.

React Vite SPA, Angular et API + frontend séparé générique ne sont pas recommandés pour le MVP actuel.
