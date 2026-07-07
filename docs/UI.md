# UI PrivateListings

Date : 2026-07-07  
Base de référence : `docs/METRONIC-REVIEW.md`, `docs/UI-DECISIONS.md` et `E:\Assets\metronic-v9.5.0`.

## Layout retenu

L'interface reprend une adaptation sobre de Metronic v9.5.0, inspirée de Demo 3 Compact Sidebar, sans importer une démo complète.

- Desktop : sidebar fixe à gauche, topbar sticky avec recherche d'annonces et utilisateur connecté, contenu centré par surface métier.
- Mobile : topbar compacte avec marque et recherche, menu mobile en tiroir léger via `<details>`, navigation basse pour les parcours fréquents.
- Navigation principale : Tableau de bord, Annonces, Publier, Messages, Profil, Administration pour les rôles `Admin` ou `Moderator`.
- Administration : navigation secondaire dédiée, tables natives Blazor avec défilement horizontal sur petit écran.

## Assets copiés

Aucun asset Metronic n'a été copié pour cette intégration.

Un asset propre au projet a été créé :

- `src/PrivateListings.Web/wwwroot/favicon.svg` : favicon SVG minimal PrivateListings, créé pour éviter de reprendre les logos Metronic.

Décision :

- les logos Metronic ne doivent pas devenir l'identité PrivateListings ;
- les illustrations disponibles sont volumineuses et non nécessaires aux états vides actuels ;
- les vendors `ktui`, `jquery`, `datatables-net`, `apexcharts`, `dropzone`, `tinymce`, `leaflet` et autres ne sont pas requis par les composants Razor créés ;
- le CSS applicatif direct suffit pour ce lot.

## Assets explicitement non copiés

Les éléments suivants restent hors dépôt :

- `metronic-tailwind-html-demos/dist/html` et toutes les démos complètes ;
- `metronic-tailwind-html-starter-kit/dist/html` ;
- les projets React, Next.js, landings et concepts ;
- les vendors JavaScript Metronic et tiers ;
- les dossiers `media` complets ;
- le fichier Figma ;
- toute archive ou paquet complet Metronic.

Ce choix respecte la licence Metronic : le dépôt ne redistribue pas inutilement les sources, les démos ou le paquet complet.

## Stratégie CSS et Tailwind

Le build CSS reste volontairement simple :

- `src/PrivateListings.Web/wwwroot/app.css` contient la couche UI applicative ;
- aucun pipeline Tailwind, Vite, Webpack ou PostCSS n'est ajouté pour l'instant ;
- aucun script global Metronic ou KTUI n'est chargé ;
- les interactions visibles sont portées par Blazor, HTML natif et formulaires serveur ;
- les composants utilisent des classes stables, documentées par leur nommage : `app-*`, `page-*`, `listing-*`, `message-*`, `profile-*`, `admin-*`.

Tailwind pourra être introduit plus tard si le besoin devient mesurable, en gardant une sortie unique et un scan limité aux composants Razor.

## Conventions composants

Les composants UI sont propriétaires au projet et ne collent pas le HTML généré par Metronic.

- Layout : `AppShell`, `Sidebar`, `Topbar`, `MobileNavigation`, `PageHeader`.
- Partagés : `StatusBadge`, `EmptyState`, `ConfirmDialog`.
- Annonces : `ListingCard`, `ListingGrid`, `ListingFilters`, `ListingDetails`, `ListingForm`.
- Messagerie : `ConversationList`, `MessageThread`.
- Profil : `ProfileCard`.
- Administration : `AdminTable`, `AdminShell`, `AdminPagination`.

Règles retenues :

- garder la logique métier dans les pages ou services existants, pas dans les composants purement UI ;
- passer les données aux composants via paramètres typés ;
- conserver les `form method="post"` et `AntiforgeryToken` existants ;
- utiliser des labels explicites pour les champs ;
- préserver un focus visible et des contrastes corrects ;
- limiter les rayons à `8px` maximum ;
- éviter les dépendances lourdes tant qu'un composant Razor suffit.

## Limites connues

- Les icônes sont dessinées en CSS pour éviter une dépendance supplémentaire ; une bibliothèque d'icônes cohérente pourra être choisie plus tard.
- Les menus et confirmations reposent sur HTML natif, sans gestion avancée du focus comme une modale JavaScript complète.
- Les tables admin restent des tables avec défilement horizontal sur mobile ; une vue cartes mobile pourra être ajoutée si l'usage admin mobile devient prioritaire.
- Aucun thème sombre n'est livré.
- Aucun asset Metronic n'est embarqué ; si une identité visuelle ou des illustrations sont nécessaires, il faudra les produire pour PrivateListings plutôt que reprendre les logos Metronic.
