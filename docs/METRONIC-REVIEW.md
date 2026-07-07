# Revue Metronic v9.5.0 pour PrivateListings

Date de revue : 2026-07-07  
Source locale inspectée : `E:\Assets\metronic-v9.5.0`  
Projet cible : PrivateListings, application Blazor Web App responsive pour annonces privées, entraide locale, services, événements, messagerie et administration.

## Synthèse

Metronic v9.5.0 est exploitable pour PrivateListings, mais seulement comme bibliothèque de motifs visuels et de composants Tailwind/KTUI. Le meilleur choix n’est pas de reprendre une démo entière : l’UX cible doit combiner une structure applicative sobre inspirée de Demo 3 Compact Sidebar, des cartes et filtres inspirés de Retail Store et Real Estate, des profils simplifiés à partir de Public Profile/User Profile, et des pages admin/sécurité adaptées depuis Team Members et Security Log.

La source prioritaire pour Blazor est le paquet HTML/Tailwind, car il expose le DOM, les classes Tailwind et les attributs KTUI sans dépendance React. Les concepts React/Next.js Mail et Real Estate sont utiles pour la logique d’expérience, mais doivent être réécrits en Razor.

## Hypothèses

- PrivateListings doit privilégier la consultation d’annonces, la publication, la prise de contact et la confiance entre membres.
- Le MVP ne doit pas ressembler à un tableau de bord SaaS généraliste.
- L’application doit donner la priorité au mobile sur les parcours publics et membre, avec une navigation desktop plus structurée pour l’administration.
- Le dépôt actuel ne contient pas encore d’application Blazor exploitable ; cette revue ne modifie donc aucun code applicatif.

## Paquets et variantes disponibles

Le dossier local contient les éléments suivants, sans arborescence exhaustive :

| Paquet | Variante | Contenu observé | Priorité Blazor |
|---|---|---:|---|
| `metronic-tailwind-html-demos` | HTML + Tailwind 4 + KTUI | 10 démos, 1 174 pages HTML environ, assets complets | Très haute |
| `metronic-tailwind-html-starter-kit` | HTML + Tailwind 4 + KTUI | 17 layouts starter, dont `layout-1/dark-sidebar` | Très haute |
| `metronic-tailwind-react-demos` | React Vite et Next.js, JS/TS | Démos React complètes, composants de cartes, profils, store, sécurité | Moyenne, comme référence |
| `metronic-tailwind-react-starter-kit` | React Vite et Next.js, JS/TS | Starter React lourd, nombreuses dépendances UI | Faible pour Blazor |
| `metronic-tailwind-react-concepts` | React Vite et Next.js, JS/TS | Concepts `ai`, `api`, `calendar`, `crm`, `mail`, `real-estate`, `store-inventory`, `todo` | Moyenne, comme référence UX |
| `metronic-tailwind-nextjs-landings` | Next.js landing SaaS | Landing marketing SaaS | Faible |
| `figma` | Design source | `Metronic_v9.5.0.fig`, environ 147 Mo | Référence design uniquement |
| `download_full_v8_bootstrap_package.txt` | Bootstrap v8 séparé | Le paquet Bootstrap complet n’est pas inclus localement | À écarter |

Le fichier `README.md` local confirme que Metronic 9.x est la branche Tailwind, tandis que Metronic 8.x Bootstrap est séparé et téléchargeable à part. Pour PrivateListings, il faut rester sur Tailwind/KTUI afin d’éviter un mélange Bootstrap/Tailwind difficile à maintenir.

## Démos HTML disponibles

Le paquet HTML complet expose `demo1` à `demo10`.

| Démo | Pages HTML observées | Lecture UX |
|---|---:|---|
| `demo1` | 122 | Layout par défaut, très complet, sidebar classique |
| `demo2` | 119 | Variante proche, moins prioritaire |
| `demo3` | 119 | Compact Sidebar, très intéressant pour une application métier sobre |
| `demo4` | 120 | Variante tableau de bord, pas prioritaire |
| `demo5` | 120 | Variante tableau de bord, pas prioritaire |
| `demo6` | 121 | Variante tableau de bord, pas prioritaire |
| `demo7` | 119 | Variante tableau de bord/store, pas prioritaire |
| `demo8` | 94 | Démo plus réduite, intérêt limité |
| `demo9` | 119 | Variante tableau de bord, pas prioritaire |
| `demo10` | 121 | Dark Sidebar, intéressant pour admin mais trop marqué pour le produit principal |

Dans `demo1`, les grandes sections observées sont : `account`, `authentication`, `dashboards`, `network`, `plugins`, `public-profile`, `security`, `store-client`, `user-table`. Cette couverture suffit pour construire les parcours membre, sécurité, annuaire/admin et annonces sans importer une démo entière.

## Starter kits HTML

Le starter HTML expose `layout-1` à `layout-17`, plus `layout-1/dark-sidebar`.

Les layouts starter sont utiles comme pages de référence plus petites que les démos complètes. Les mesures locales montrent cependant que certains layouts restent chargés, car ils embarquent les mêmes zones de notifications, recherche globale, modales et exemples :

| Layout starter | Taille HTML | Intérêt |
|---|---:|---|
| `layout-1` | 330 Ko | Shell sidebar classique, utile comme base d’extraction |
| `layout-1/dark-sidebar` | 331 Ko | Variante sombre, utile pour comparaison admin |
| `layout-3` | 170 Ko | Plus compact, meilleur candidat starter |
| `layout-10` | 200 Ko | Variante sombre ou structurée, utile mais moins sobre |
| `layout-11` | 22 Ko | Layout très léger, intéressant pour pages publiques simples |
| `layout-12` | 38 Ko | Layout léger, intéressant pour pages de compte ou auth |

Recommandation : utiliser `layout-3` et `layout-11/12` comme références structurelles, mais conserver Demo 3 comme choix principal de shell desktop.

## Assets et dépendances observés

Dans `metronic-tailwind-html-demos/dist/assets` :

| Sous-dossier | Fichiers | Taille |
|---|---:|---:|
| `assets/css` | 2 | 0,52 Mo |
| `assets/js` | 12 | 1,60 Mo |
| `assets/vendors` | 298 | 19,20 Mo |
| `assets/media` | 682 | 20,62 Mo |

Extensions principales des assets HTML : 521 SVG, 199 JS, 101 PNG, 68 JPG, 52 CSS, 44 TS, quelques polices et favicon.

Vendors disponibles : `@form-validation`, `apexcharts`, `canvas-confetti`, `clipboard`, `datatables-net`, `dropzone`, `fullcalendar`, `jquery`, `keenicons`, `ktui`, `leaflet`, `prismjs`, `tinymce`.

Pour PrivateListings, le socle utile est limité à :

- `assets/css/styles.css`, ou mieux une génération Tailwind dédiée ;
- `assets/vendors/ktui/ktui.min.js` si les attributs KTUI sont utilisés ;
- `assets/vendors/keenicons/styles.bundle.css` et les polices Keenicons si les icônes Metronic sont conservées ;
- quelques assets `media/app` pour favicon/logo si validés ;
- quelques illustrations seulement si elles servent vraiment un état vide ou une page sécurité.

À éviter pour le MVP : `apexcharts`, `datatables-net`, `jquery`, `fullcalendar`, `leaflet`, `tinymce`, `dropzone`, `canvas-confetti`, sauf besoin métier explicite.

## Comparaison des templates pertinents

Échelle de complexité : faible, moyenne, élevée.  
Quantité d’assets : faible, moyenne, élevée, selon le nombre d’images/vendors nécessaires.

| Template | Source locale | Usage possible dans PrivateListings | Intérêt ergonomique | Complexité | Mobile | Assets | Risque de surcharge UI | Verdict |
|---|---|---|---|---|---|---|---|---|
| Demo 1 Default | `dist/html/demo1/index.html` | Référence générale de layout, compte, sécurité, navigation | Sidebar classique lisible, composants complets | Moyenne à élevée | Bonne via drawer | Élevée | Très chargé en notifications, modales et blocs secondaires | À utiliser comme catalogue, pas comme structure applicative |
| Demo 3 Compact Sidebar | `dist/html/demo3/index.html` | Structure desktop principale | Sidebar 58 px, contenu plus respirant, bonne densité | Moyenne | Bonne via drawer | Moyenne | Peut rester trop tableau de bord si la barre secondaire est reprise telle quelle | Retenu comme base desktop |
| Demo 10 Dark Sidebar | `dist/html/demo10/index.html` | Administration avancée ou back-office futur | Contraste fort, séparation admin nette | Moyenne | Correcte | Moyenne | Trop sombre et SaaS pour l’espace membre public | À réserver à l’admin, pas au MVP public |
| Starter Layout 3 | `html-starter-kit/dist/html/layout-3/index.html` | Base technique de shell compact | Plus léger que Demo 1 | Moyenne | Correcte | Moyenne | Exemples encore génériques | Référence de simplification |
| Starter Layout 11/12 | `html-starter-kit/dist/html/layout-11`, `layout-12` | Pages publiques, auth, pages simples | Très léger, peu de bruit visuel | Faible | Bonne | Faible | Moins complet pour une app connectée | À utiliser pour pages publiques/auth |
| Projects Columns | `demo1/public-profile/projects/3-columns.html`, `2-columns.html` | Grille d’annonces ou services en cartes | Structure de cartes et grille réactive | Moyenne | Bonne | Élevée dans la page complète, faible si cartes isolées | Vocabulaire projet trop B2B | À adapter fortement |
| Projects Rows | `react-demos/.../card-project-row.tsx`, `card-project-extended-row.tsx` | Liste d’annonces compacte, favoris, demandes, événements | Très bon pattern liste/tablette | Faible à moyenne si réécrit | Bonne si cartes empilées | Faible | Composants React non copiables tels quels | Réécrire en Razor |
| Public Profile | `demo1/public-profile/profiles/plain.html`, `default.html` | Profil public membre, confiance, annonces du membre | Header profil, badges, sections “À propos” | Moyenne | Bonne | Moyenne | Trop de métriques vanity et actions sociales | Garder `plain`, simplifier |
| User Profile | `demo1/account/home/user-profile.html` | Espace compte membre | Données personnelles et blocs structurés | Moyenne | Bonne | Moyenne | Beaucoup de widgets inutiles | Source secondaire |
| Account Settings | `demo1/account/home/settings-sidebar.html` | Paramètres compte et sécurité | Très bon scrollspy desktop | Élevée si tout est repris | Correcte, mais sidebar masquée mobile | Élevée | Trop de champs, Dropzone, date picker, TinyMCE | Reprendre la structure, pas les widgets |
| Sign In Form | `demo1/authentication/classic/sign-in.html` | Connexion | Carte simple, lisible, faible poids | Faible | Très bonne | Faible | Image de fond et social login à valider | Retenu, simplifié |
| 2-factor Authentication | `demo1/authentication/classic/2fa.html` | Validation 2FA ou code de connexion | Pin input accessible avec labels | Faible à moyenne | Bonne | Faible | Dépendance KTUI pin input si non réécrit | Retenu, réécrire la logique |
| Security Overview | `demo1/account/security/overview.html` | Sécurité du compte | Bon inventaire des réglages, toggles, état 2FA | Moyenne | Bonne | Moyenne | Texte et options trop SaaS/Pro | Retenu, réduire |
| Security Logs | `demo1/account/security/security-log.html` | Journal d’audit compte/admin | Table claire avec sévérité | Moyenne | Scroll horizontal requis | Moyenne | DataTable inutile et colonnes réseau trop techniques | Retenu avec table Blazor native |
| User Management | `demo1/account/members/team-members.html` | Admin utilisateurs | Table, recherche, statuts, rôles | Moyenne | Correcte via table responsive | Moyenne | Terminologie équipe/B2B | Retenu après renommage métier |
| Team Members DataTables | `team-members-datatable.html` | À première vue admin utilisateurs | Tri/pagination prêt à l’emploi | Élevée | Correcte | Élevée | DataTables + jQuery, trop lourd | À écarter |
| Mail | `react-concepts/typescript/nextjs/app/mail` | Messagerie privée | Liste + fil, excellent pattern | Élevée si copié, moyenne si réécrit | Très bonne avec état liste/détail | Moyenne | React, contexte, toasts, composants Radix | Réécrire en Razor |
| Retail Store | `demo1/store-client/search-results-grid.html`, `product-details.html` | Liste d’annonces, filtres, cartes | Grille/list switch, filtres drawer, cartes visuelles | Moyenne | Bonne | Moyenne à élevée | Panier, wishlist, achat et drawers e-commerce inutiles | Retenir cartes/filtres seulement |
| Real Estate | `react-concepts/typescript/nextjs/app/real-estate` | Annonces immobilières ou biens localisés | Galerie, favoris, carte, filtres localisation | Élevée | Moyenne à bonne selon adaptation | Élevée si carte | React Leaflet, split 50/50 trop spécifique | Référence UX, pas copie |

## Templates à écarter pour le MVP

À écarter explicitement :

- Les tableaux de bord complets des démos, car ils injectent des graphiques, notifications, tiroirs et métriques qui ne servent pas la découverte d’annonces.
- Demo 10 comme layout principal, car le dark sidebar donne une tonalité back-office trop forte pour un produit de proximité.
- Les pages `billing`, `api-keys`, `integrations`, `campaigns`, `crm`, `ai`, `api`, `calendar`, `todo`, sauf évolution métier ultérieure.
- Les variantes DataTables, car elles ajoutent `jquery` et `datatables-net` alors que Blazor peut gérer tri, pagination et virtualisation.
- Les pages e-commerce Retail Store complètes, notamment panier, wishlist, checkout, paiement et commandes.
- Le Store Inventory React Concept, trop orienté stock, commandes, factures et logistique.
- Les landings Next.js SaaS, trop marketing et hors sujet pour l’application connectée.
- Le paquet Bootstrap v8, non inclus localement et inutile si l’application part sur Tailwind.

## Choix final recommandé

| Surface PrivateListings | Template retenu | Recommandation |
|---|---|---|
| Layout principal desktop | Demo 3 Compact Sidebar | Structure compacte, simplifiée, avec sidebar d’icônes et topbar de recherche. Ne pas reprendre les blocs de tableau de bord. |
| Layout mobile | Drawer mobile Demo 3 + navigation mobile dédiée | Topbar légère, recherche visible, navigation basse pour les parcours clés, drawer pour paramètres/admin. |
| Page liste annonces | Retail Store Search Grid/List + Real Estate Card | Grille desktop, liste mobile, filtres drawer, tri, favoris, badges de catégorie. Supprimer tout panier/achat. |
| Page détail annonce | Real Estate Card + Retail Product Details | Galerie, informations, localisation optionnelle, contact vendeur, signalement. Réécrire la galerie en Blazor. |
| Page profil membre | Public Profile Plain + User Profile | Profil sobre avec avatar, vérifications, bio, annonces publiées, notes de confiance. Supprimer métriques B2B. |
| Page messagerie | React Concepts Mail | Recréer en Razor : liste conversations, fil, composer, état mobile liste/détail. |
| Page admin utilisateurs | Team Members sans DataTables | Table Blazor native : recherche, filtres, rôles, statut, actions. |
| Page sécurité du compte | Security Overview + Account Settings | Synthèse sécurité, 2FA, sessions, mot de passe, appareils. |
| Page logs/audit | Security Logs | Table d’événements adaptée : date, acteur, action, cible, IP, résultat, sévérité. |

## Stratégie d’intégration Blazor

### Fichiers à copier ou référencer

Option recommandée en dépôt privé :

- Créer un dossier source de thème isolé, par exemple `src/PrivateListings.Web/ClientAssets/metronic`.
- Copier uniquement le nécessaire depuis `metronic-tailwind-html-demos` ou `metronic-tailwind-html-starter-kit` :
  - `src/css/styles.css`;
  - `src/css/config.ktui.css`;
  - `src/css/components/index.css` et les composants réellement utilisés ;
  - `src/vendors/keenicons` si Keenicons est retenu ;
  - `src/core` uniquement si `KTComponent.init` et les comportements KTUI sont nécessaires ;
  - `package.json`, `postcss.config.js`, `webpack.config.js`, `webpack.vendors.js` si le build Metronic est conservé.
- Sortie build vers `src/PrivateListings.Web/wwwroot/assets/metronic`.

Option recommandée si le dépôt est public :

- Ne pas commiter le dossier source Metronic.
- Garder Metronic hors dépôt ou dans un registre/artefact privé.
- Documenter un script de restauration local non public.
- Ne commiter que les composants Razor écrits par le projet et, si la licence le permet pour le déploiement, les assets générés strictement nécessaires.

### Fichiers à ne pas copier

- `dist/html` complet.
- Les 10 démos complètes.
- Les assets `media` complets.
- Les vendors non utilisés : `apexcharts`, `datatables-net`, `jquery`, `fullcalendar`, `leaflet`, `tinymce`, `dropzone`, `canvas-confetti`, `prismjs`.
- Les projets React, Next.js et landing dans l’application Blazor.
- Le fichier Figma.

### Organisation Razor proposée

Créer des composants Razor propriétaires plutôt que de coller le HTML généré :

- `Components/Layout/AppShell.razor`
- `Components/Layout/AppHeader.razor`
- `Components/Layout/DesktopSidebar.razor`
- `Components/Layout/MobileTopBar.razor`
- `Components/Layout/MobileBottomNav.razor`
- `Components/Layout/SecondaryDrawer.razor`
- `Components/Shared/PageToolbar.razor`
- `Components/Shared/SearchBox.razor`
- `Components/Shared/StatusBadge.razor`
- `Components/Shared/EmptyState.razor`
- `Components/Listings/ListingCard.razor`
- `Components/Listings/ListingGrid.razor`
- `Components/Listings/ListingList.razor`
- `Components/Listings/ListingFilters.razor`
- `Components/Listings/ListingFilterDrawer.razor`
- `Components/Listings/ListingGallery.razor`
- `Components/Listings/ListingDetailHeader.razor`
- `Components/Profile/MemberProfileHeader.razor`
- `Components/Profile/MemberTrustSummary.razor`
- `Components/Profile/MemberListings.razor`
- `Components/Messaging/ConversationLayout.razor`
- `Components/Messaging/ConversationList.razor`
- `Components/Messaging/ConversationThread.razor`
- `Components/Messaging/MessageComposer.razor`
- `Components/Admin/AdminUserTable.razor`
- `Components/Admin/UserRoleBadge.razor`
- `Components/Security/AccountSecurityOverview.razor`
- `Components/Security/TwoFactorPanel.razor`
- `Components/Security/AuditLogTable.razor`

### Tailwind, Vite et build

Metronic v9 utilise Tailwind 4. La documentation officielle Blazor Server Metronic recommande de copier le dossier source Metronic dans le projet, de copier `media` vers `wwwroot`, d’ajuster les sorties JS/CSS vers `wwwroot`, puis d’appeler `KTComponent.init` et `KTLayout.init` après le rendu. Pour PrivateListings, cette approche doit être resserrée.

Recommandation :

- Utiliser Tailwind 4 au niveau du projet Blazor.
- Scanner explicitement les fichiers `.razor`, `.cshtml`, `.html` et les sources KTUI nécessaires.
- Générer un CSS unique vers `wwwroot/assets/css/app.css`.
- Utiliser Vite seulement si un bundle JS applicatif est nécessaire.
- Garder la logique interactive principale dans Blazor.
- Appeler KTUI seulement pour les composants non réécrits au début, puis réduire progressivement cette dépendance.

Exemple d’intention de build, à adapter quand l’application Blazor existera :

```json
{
  "scripts": {
    "build:css": "npx @tailwindcss/cli -i ./Styles/app.css -o ./wwwroot/assets/css/app.css --minify",
    "watch:css": "npx @tailwindcss/cli -i ./Styles/app.css -o ./wwwroot/assets/css/app.css --watch"
  }
}
```

Dans `Styles/app.css`, prévoir des sources de scan du type :

```css
@import "tailwindcss";
@source "../Components/**/*.razor";
@source "../Pages/**/*.razor";
@source "../wwwroot/**/*.html";
@source "../ClientAssets/metronic/node_modules/@keenthemes/ktui/src/components/";
@import "../ClientAssets/metronic/src/css/config.ktui.css";
@import "../ClientAssets/metronic/node_modules/@keenthemes/ktui/styles.css";
```

### Dépendances à éviter

- `jquery` et `datatables-net` : remplacer par composants Blazor.
- `apexcharts` et `recharts` : pas nécessaires au MVP annonces.
- `fullcalendar` : inutile tant que les événements restent de simples annonces datées.
- `leaflet` ou `react-leaflet` : reporter la carte interactive ; commencer par une localisation textuelle ou une carte optionnelle légère.
- `tinymce` : remplacer par un champ texte enrichi minimal ou Markdown si nécessaire.
- `dropzone` : utiliser l’upload Blazor/.NET, puis améliorer plus tard.
- React, Next.js, Radix, Sonner, Motion : références UI seulement, pas de dépendance runtime Blazor.

### Composants à réécrire plutôt que copier

- Sidebar, topbar, menus, dropdowns et drawers : mieux contrôlés en Blazor.
- Tables admin et audit : tri, filtres, pagination et sélection natifs Blazor.
- Formulaires auth, 2FA et settings : validation .NET, accessibilité et antiforgery.
- Galerie annonce : composant Razor dédié.
- Messagerie : état liste/détail, lecture, envoi et brouillon gérés côté Blazor.
- Filtres annonces : état URL/query string plutôt que menus KTUI opaques.

## Licence et prudence de distribution

Sources consultées :

- fichier local `LICENSE-REMINDER.txt` ;
- fichier local `metronic-tailwind-html-demos/LICENSE-NOTICE.txt`;
- documentation officielle Metronic : `https://keenthemes.com/metronic/tailwind/docs/getting-started/license`.

Points à retenir :

- une licence Metronic couvre un seul produit ou déploiement de production ;
- une application SaaS payante nécessite une licence Extended ;
- la documentation officielle indique que Metronic ne peut pas être utilisé pour un projet open source et que le code source incluant Metronic ne doit pas être distribué publiquement ;
- les environnements de test et développement sont couverts autour d’un même produit, mais chaque déploiement de production distinct nécessite sa propre licence.

Conséquence pour PrivateListings :

- ne pas copier l’intégralité de `E:\Assets\metronic-v9.5.0` dans le dépôt ;
- si le dépôt devient public, ne pas y commiter les sources Metronic, les démos, le Figma ou les assets complets ;
- garder une preuve de licence et valider le type de licence avant une mise en production payante ;
- copier seulement les assets nécessaires au produit final, en documentant leur origine.

## Risques résiduels

- Les pages HTML générées sont volumineuses ; les copier-coller directement produirait un code Razor difficile à maintenir.
- Les comportements KTUI basés sur attributs HTML peuvent entrer en friction avec l’état Blazor si trop de composants restent pilotés par JavaScript.
- La tentation d’importer trop d’assets Metronic est forte ; il faut un inventaire par page.
- Les textes et patterns Metronic sont très SaaS/B2B ; il faut les réécrire pour une application locale et communautaire.
- La licence impose une discipline forte si le dépôt est public ou si PrivateListings devient un service payant.
