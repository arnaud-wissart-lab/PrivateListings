# Produit

## Vision

PrivateListings est une plateforme web responsive d'annonces privées et semi-privées orientée entraide locale. Elle donne à une communauté un espace maîtrisé pour publier des annonces, proposer des services, demander de l'aide, organiser des événements et échanger sans exposer inutilement les données personnelles.

Le produit doit rester simple à comprendre, fiable dans les usages quotidiens et suffisamment administrable pour éviter que la modération devienne une charge opaque. La confiance, la lisibilité et la sécurité priment sur la quantité de fonctionnalités.

## Public cible

- Communautés de voisinage.
- Associations, clubs et collectifs locaux.
- Petites organisations qui veulent un espace interne d'échange.
- Groupes semi-privés avec validation des membres.

## Baseline produit

La baseline initiale est centrée sur les annonces privées et l'entraide locale :

- Publier une annonce visible par les membres autorisés.
- Rechercher et filtrer les annonces.
- Répondre à une annonce via messagerie interne.
- Proposer un service ou demander de l'aide.
- Créer un événement local simple.
- Signaler un contenu ou un comportement.
- Administrer les membres, les annonces et les signalements.

## Principes produit

- Accès privé ou semi-privé par défaut.
- Données personnelles limitées au nécessaire.
- Parcours mobile aussi important que le desktop.
- Modération visible, traçable et proportionnée.
- Interface française claire, sans jargon technique.
- Pas de dépendance au courrier électronique pour transmettre des secrets.
- Mode démo isolé, avec données fictives uniquement.

## MVP

Le MVP doit livrer une plateforme utilisable de bout en bout, sans couvrir tous les scénarios avancés.

### Comptes et accès

- Inscription avec validation par lien temporaire à usage unique.
- Connexion sécurisée.
- Réinitialisation de mot de passe par lien temporaire à usage unique.
- Profil membre minimal.
- Gestion du statut de compte : actif, en attente, suspendu, supprimé ou anonymisé.
- Rôles applicatifs : visiteur, membre, modérateur, administrateur.

### Annonces

- Création, modification par propriétaire, publication, retour en brouillon et suppression logique d'annonce.
- Types MVP :
  - je vends ;
  - je donne ;
  - je cherche ;
  - je propose un service ;
  - événement ;
  - emploi ou mission ;
  - projet ou entraide.
- Titre, description, type, catégorie, localisation approximative, prix facultatif, date d'événement facultative, date d'expiration facultative, images, statut et dates.
- Validation stricte du contenu : pas de HTML libre.
- Upload d'images uniquement pour le MVP, avec validation serveur et stockage contrôlé.
- Recherche textuelle simple et filtres par type, catégorie, localisation approximative, prix et date d'événement.
- Les annonces expirées, suspendues ou supprimées ne sont pas visibles dans la liste publique.

### Messagerie interne

- Conversation liée à une annonce.
- Messages entre membres authentifiés.
- Blocage des conversations pour comptes suspendus ou annonces supprimées.
- Signalement d'un message.
- Conservation cohérente avec les règles de confidentialité et d'audit.

### Événements

- Création d'un événement simple : titre, description, date, lieu approximatif, nombre de places facultatif.
- Affichage dans les annonces et filtres.
- Réponse ou prise de contact via messagerie interne.

### Modération

- Liste des annonces signalées.
- Liste des messages signalés.
- Actions de modération : masquer, restaurer, archiver, suspendre un compte.
- Journal d'audit pour les actions sensibles.
- Motif obligatoire pour les actions de modération structurantes.

### Administration

- Gestion des membres.
- Gestion des rôles.
- Vue d'ensemble des contenus récents et signalés.
- Configuration minimale de la plateforme : nom de communauté, mode d'inscription, coordonnées de contact.
- Mode démo isolé avec jeu de données fictif.

## Hors périmètre MVP

- Paiement en ligne.
- Système d'enchères.
- Géolocalisation précise en temps réel.
- Chat instantané temps réel avancé.
- Application mobile native.
- Marketplace publique indexable par les moteurs de recherche.
- HTML riche libre dans les annonces.
- Upload de documents autres que les images.
- Multi-tenant complexe avec facturation.

## Rôles

### Visiteur

Utilisateur non authentifié. Il peut accéder aux pages publiques minimales, comme la connexion, l'inscription, les informations légales et le contact. Il ne doit pas accéder aux annonces privées.

### Membre

Utilisateur authentifié et activé. Il peut créer et gérer ses annonces, répondre aux annonces autorisées, participer aux conversations, signaler un contenu et gérer son profil.

### Modérateur

Membre de confiance chargé de traiter les signalements, masquer les contenus problématiques, restaurer des contenus, suspendre temporairement certains accès selon les droits accordés et documenter ses décisions.

### Administrateur

Responsable de la configuration de la plateforme, des rôles, des politiques de modération, des exports, des suppressions et des actions sensibles. Ses actions doivent être auditées.

## UX cible

L'interface doit être efficace, lisible et rassurante.

- Navigation principale simple : annonces, publier, messages, événements, profil, administration si autorisée.
- Tableau de bord membre orienté action : annonces récentes, conversations actives, annonces personnelles.
- Formulaire de publication court, avec validations explicites et brouillon si utile.
- Affichage des annonces sous forme de liste ou cartes compactes, adapté mobile et desktop.
- Filtres visibles et faciles à réinitialiser.
- États vides utiles, sans discours marketing.
- Erreurs compréhensibles, sans divulguer d'informations sensibles.
- Confirmations explicites avant suppression, archivage ou action de modération.
- Accessibilité prise en compte dès le MVP : navigation clavier, labels, contrastes et tailles tactiles correctes.

## Indicateurs de qualité produit

- Un membre peut publier une annonce complète en moins de deux minutes.
- Un membre comprend clairement qui peut voir son annonce.
- Un modérateur peut retrouver rapidement les contenus signalés et l'historique de décision.
- Les parcours d'inscription, d'activation, de connexion et de reset restent robustes face aux abus.
- L'interface reste utilisable sur mobile sans perte fonctionnelle majeure.
