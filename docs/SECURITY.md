# Sécurité

## Objectif

PrivateListings manipule des comptes, annonces privées, messages internes et actions de modération. La sécurité doit être intégrée dès la conception pour protéger les membres, limiter les abus et conserver une trace fiable des actions sensibles.

## Principes

- Défense en profondeur.
- Moindre privilège.
- Données sensibles minimisées.
- Secrets jamais exposés dans les logs, emails ou messages d'erreur.
- Validation serveur systématique.
- Actions sensibles auditées.
- Comportement sécurisé par défaut.

## Authentification

- Aucun mot de passe ne doit être envoyé par email.
- Les mots de passe sont hachés par ASP.NET Core Identity.
- La politique actuelle accepte les mots de passe longs de gestionnaire de mots de passe : minimum 12 caractères, maximum 256, sans obligation artificielle de chiffres, majuscules ou symboles.
- Activation de compte par lien temporaire à usage unique.
- Réinitialisation de mot de passe par lien temporaire à usage unique.
- Les jetons temporaires sont :
  - générés par les fournisseurs de jetons Identity ;
  - stockés sous forme hachée dans `account_security_tokens` ;
  - limités à 2 heures ;
  - invalidés après usage ;
  - invalidés par rotation lorsqu'un nouveau lien du même type est généré.
- Le worker purge périodiquement les entrées expirées de `account_security_tokens`.
- Les messages d'erreur de connexion ne doivent pas révéler si un email existe.
- Un changement d'email ne modifie pas immédiatement le compte : un lien de confirmation est envoyé à la nouvelle adresse et l'ancienne adresse reste active jusqu'à confirmation.

## Sessions et cookies

- Cookies d'authentification `Secure`, `HttpOnly` et `SameSite=Lax`.
- Durée de session raisonnable et configurable.
- Renouvellement contrôlé des sessions.
- Invalidation de session après changement de mot de passe.
- Protection contre la fixation de session.
- Les clés ASP.NET Core Data Protection utilisent l'application stable `PrivateListings` et doivent être persistées via `PrivateListings:DataProtection:KeysPath` en Docker.

## Headers HTTP

- Les réponses applicatives ajoutent `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy` et `Permissions-Policy`.
- Une CSP progressive est activée par défaut via `Content-Security-Policy`.
- La valeur CSP est configurable par `PrivateListings:SecurityHeaders:ContentSecurityPolicy`.
- La politique actuelle reste compatible Blazor Server, notamment pour la connexion SignalR et les styles/scripts inline existants.

## Autorisation

- Les accès doivent être contrôlés par politiques explicites.
- Les rôles MVP sont : membre, modérateur, administrateur.
- Les actions de modération et d'administration doivent être protégées par autorisation serveur.
- Le back-office MVP utilise deux policies serveur :
  - `BackOffice.Admin`, réservée au rôle `Admin` ;
  - `BackOffice.Moderation`, réservée aux rôles `Admin` et `Moderator`.
- Les vérifications d'autorisation ne doivent jamais dépendre uniquement de l'UI.
- Un membre ne doit accéder qu'aux annonces, conversations et fichiers qu'il est autorisé à consulter.

## Protection CSRF

Toutes les actions modifiant l'état doivent être protégées contre la falsification de requête intersite.

Exemples :

- création ou modification d'annonce ;
- envoi de message ;
- signalement ;
- action de modération ;
- modification de profil ;
- changement de mot de passe ;
- suppression ou anonymisation.

## Rate limiting

Un rate limiting applicatif en mémoire est appliqué sur les parcours compte suivants :

- connexion ;
- inscription ;
- activation de compte ;
- demande de reset password ;
- reset password.

Le rate limiting du module contact reste à ajouter lorsque ce parcours sera implémenté.

Le module annonces applique maintenant un rate limiting applicatif en mémoire sur :

- création d'annonce ;
- upload d'image d'annonce.

Les limites doivent tenir compte de l'adresse IP, du compte ou de l'email normalisé selon le cas, sans stocker plus de données que nécessaire.

Le rate limiting actuel hache la clé interne composée de l'action, de l'identifiant normalisé et de l'adresse IP. Une implémentation distribuée restera nécessaire avant un déploiement multi-instance.

## Validation des entrées

- Validation serveur obligatoire pour toutes les entrées.
- Validation client uniquement comme aide ergonomique.
- Pas de HTML libre dans les annonces, profils, messages ou commentaires.
- Normalisation des emails.
- Longueurs maximales explicites pour les champs texte.
- Rejet des caractères de contrôle inutiles.
- Encodage de sortie systématique dans l'UI.

## Uploads

Le MVP autorise uniquement les images.

Exigences :

- validation de l'extension ;
- validation du type MIME ;
- validation de la signature réelle du fichier ;
- limite de taille ;
- renommage serveur ;
- stockage hors webroot ;
- endpoint contrôlé pour servir les fichiers autorisés ;
- absence de confiance dans le nom de fichier original ;
- refus explicite des SVG ;
- suppression opportuniste des métadonnées connues lorsque le format le permet ;
- nettoyage des fichiers orphelins ;
- journalisation sans chemin sensible.

Le redimensionnement, la recompression et la validation de dimensions ne sont pas activés dans l'implémentation actuelle, faute de dépendance d'imagerie compatible avec les contraintes de licence et de build. La limite de taille reste la barrière configurable du MVP.

## Messagerie interne

- Les messages ne doivent être visibles que par les participants autorisés.
- Les comptes suspendus ne doivent pas pouvoir initier ou poursuivre des échanges si la règle produit le prévoit.
- Les messages signalés doivent rester accessibles aux modérateurs autorisés.
- Les suppressions doivent respecter la politique de confidentialité et les obligations d'audit.

## Journalisation

Les logs techniques ne doivent pas contenir :

- mots de passe ;
- jetons d'activation ou de reset ;
- cookies ;
- secrets ;
- contenu complet de messages privés ;
- données personnelles inutiles ;
- chemins physiques sensibles.

Les logs doivent permettre le diagnostic sans exposer les membres.

## Audit log

Un journal d'audit doit tracer les actions sensibles.

Actions concernées :

- connexion réussie ;
- connexion échouée ;
- inscription ;
- activation de compte ;
- demande et usage de reset password ;
- changement de mot de passe ;
- demande et confirmation de changement d'email ;
- changement de rôle ;
- demande de suppression de compte ;
- suspension ou réactivation de compte ;
- suppression ou anonymisation ;
- action de modération ;
- export de données ;
- accès ou modification de configuration sensible.

Chaque entrée d'audit doit inclure au minimum :

- identifiant de l'acteur si disponible ;
- type d'action ;
- cible ;
- date et heure ;
- résultat ;
- motif lorsque requis ;
- identifiant de corrélation.

Le journal d'audit ne doit pas stocker de secrets. Les métadonnées techniques actuellement ajoutées aux événements compte utilisent des hachages pour l'email, l'adresse IP et le user-agent.

## Administration et modération

- Les écrans d'administration doivent être protégés par autorisation serveur.
- Les actions destructives ou sensibles doivent demander confirmation.
- Les actions de modération structurantes doivent demander un motif.
- Les administrateurs ne doivent pas utiliser de comptes partagés.
- Les changements de rôle doivent être audités.
- Les modérateurs peuvent traiter les signalements d'annonces et suspendre une annonce ; les signalements visant un profil, une conversation ou un message restent réservés aux administrateurs dans le MVP.
- Le back-office MVP ne liste pas globalement les messages privés.
- Une suspension utilisateur est enregistrée sur le compte Identity et empêche la création ou la publication d'annonce.

## Emails

- Aucun mot de passe ou secret durable dans les emails.
- Les liens d'activation et de reset doivent expirer rapidement.
- Les emails doivent rester sobres et ne pas révéler d'informations sensibles.
- Les liens doivent pointer vers `PrivateListings:PublicBaseUrl`, jamais vers le `Host` de la requête entrante.
- En développement Aspire, MailPit reçoit les emails via SMTP ; le lien complet n'est pas écrit dans les logs applicatifs.

## Mode démo

- Le mode démo doit être isolé.
- Les données de démo doivent être fictives.
- Aucun email réel ne doit être utilisé sans consentement explicite.
- Le mode démo ne doit pas partager de base ou de stockage avec la production.
- Les comptes démo ne doivent jamais donner accès aux données réelles.
- Le seed démo est désactivé par défaut et ne s'exécute jamais en production.
- Les mots de passe de démonstration sont des valeurs fictives configurées pour `Demo`; seul leur hachage PBKDF2 est stocké en base.
- Les emails de démonstration utilisent le domaine réservé `example.test`.
- Le lancement manuel du seed démo depuis le back-office est refusé hors environnement `Demo` et produit un `AuditLog`.

## Configuration et secrets

- Secrets exclus du dépôt.
- Configuration sensible fournie par variables d'environnement, secret store local ou mécanisme de déploiement adapté.
- Rotation possible des secrets.
- Valeurs par défaut de développement non réutilisées en production.
- Vérification de configuration au démarrage pour les paramètres critiques.
- En Production, le démarrage est refusé si `PrivateListings:PublicBaseUrl` est absent, si `AllowedHosts` vaut `*`, si SMTP n'est pas configuré ou si le seed démo est activé.

## Déploiement

- Images Docker construites par CI.
- Publication dans GHCR.
- Déploiement par Docker Compose.
- PostgreSQL et stockage images sur volumes persistants.
- Sauvegardes documentées.
- TLS obligatoire en production, via reverse proxy ou terminaison adaptée.

## Tests de sécurité attendus

- Activation de compte : expiration, usage unique, jeton invalide.
- Reset password : expiration, usage unique, invalidation après usage.
- Rate limiting : login, inscription, reset et contact.
- Autorisations : membre, modérateur, administrateur.
- CSRF sur actions d'écriture.
- Upload : extension, MIME, signature, taille, refus SVG et absence de chemin utilisateur.
- Absence de HTML libre rendu.
- Non-divulgation d'existence de compte.

## Réponse aux incidents

Une procédure minimale devra être documentée avant production :

- identifier la nature de l'incident ;
- préserver les logs utiles ;
- révoquer les secrets exposés ;
- suspendre les comptes compromis si nécessaire ;
- informer les personnes concernées selon les obligations applicables ;
- corriger et tester avant remise en service.
