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
- Les mots de passe doivent être hachés avec un mécanisme robuste fourni par ASP.NET Core Identity ou équivalent validé.
- Activation de compte par lien temporaire à usage unique.
- Réinitialisation de mot de passe par lien temporaire à usage unique.
- Les jetons temporaires doivent être :
  - générés avec une source cryptographiquement sûre ;
  - stockés sous forme hachée ;
  - limités dans le temps ;
  - invalidés après usage ;
  - invalidés lors d'un changement sensible de compte si pertinent.
- Les messages d'erreur de connexion ne doivent pas révéler si un email existe.

## Sessions et cookies

- Cookies d'authentification `Secure`, `HttpOnly` et `SameSite`.
- Durée de session raisonnable et configurable.
- Renouvellement contrôlé des sessions.
- Invalidation de session après changement de mot de passe.
- Protection contre la fixation de session.

## Autorisation

- Les accès doivent être contrôlés par politiques explicites.
- Les rôles MVP sont : membre, modérateur, administrateur.
- Les actions de modération et d'administration doivent être protégées par autorisation serveur.
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

Un rate limiting doit être appliqué au minimum sur :

- connexion ;
- inscription ;
- activation de compte ;
- demande de reset password ;
- contact ;
- endpoints de validation de jetons si exposés ;
- upload d'images.

Les limites doivent tenir compte de l'adresse IP, du compte ou de l'email normalisé selon le cas, sans stocker plus de données que nécessaire.

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
- limite de dimensions ;
- renommage serveur ;
- stockage hors webroot ;
- endpoint contrôlé pour servir les fichiers autorisés ;
- absence de confiance dans le nom de fichier original ;
- nettoyage des fichiers orphelins ;
- journalisation sans chemin sensible.

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

- activation de compte ;
- demande et usage de reset password ;
- changement de mot de passe ;
- changement de rôle ;
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

Le journal d'audit ne doit pas stocker de secrets.

## Administration et modération

- Les écrans d'administration doivent être protégés par autorisation serveur.
- Les actions destructives ou sensibles doivent demander confirmation.
- Les actions de modération structurantes doivent demander un motif.
- Les administrateurs ne doivent pas utiliser de comptes partagés.
- Les changements de rôle doivent être audités.

## Emails

- Aucun mot de passe ou secret durable dans les emails.
- Les liens d'activation et de reset doivent expirer rapidement.
- Les emails doivent rester sobres et ne pas révéler d'informations sensibles.
- Les liens doivent pointer vers le domaine configuré de l'application.

## Mode démo

- Le mode démo doit être isolé.
- Les données de démo doivent être fictives.
- Aucun email réel ne doit être utilisé sans consentement explicite.
- Le mode démo ne doit pas partager de base ou de stockage avec la production.
- Les comptes démo ne doivent jamais donner accès aux données réelles.

## Configuration et secrets

- Secrets exclus du dépôt.
- Configuration sensible fournie par variables d'environnement, secret store local ou mécanisme de déploiement adapté.
- Rotation possible des secrets.
- Valeurs par défaut de développement non réutilisées en production.
- Vérification de configuration au démarrage pour les paramètres critiques.

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
- Upload : extension, MIME, signature, taille et dimensions.
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
