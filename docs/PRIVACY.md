# Confidentialité

## Objectif

PrivateListings doit limiter la collecte de données personnelles, expliquer clairement les usages, permettre l'export et la suppression, et éviter toute exposition inutile des membres d'une communauté privée ou semi-privée.

Ce document fixe les principes initiaux. Il ne remplace pas une analyse juridique formelle avant mise en production.

## Principes

- Collecter uniquement les données nécessaires au service.
- Expliquer les traitements dans un langage clair.
- Donner aux membres le contrôle raisonnable sur leurs données.
- Séparer données réelles et données de démonstration.
- Éviter l'exposition publique des contenus privés.
- Conserver les données uniquement pendant une durée justifiée.
- Journaliser les actions sensibles sans stocker de secrets.

## Données collectées au MVP

### Compte

- Email.
- Mot de passe haché.
- Statut du compte.
- Date de création.
- Date d'activation.
- Rôles.
- Métadonnées techniques limitées nécessaires à la sécurité.

### Profil membre

- Nom affiché ou pseudonyme.
- Informations facultatives de présentation.
- Préférences minimales de notification si elles sont ajoutées.

### Annonces

- Titre.
- Description.
- Type d'annonce.
- Catégorie.
- Localisation approximative.
- Prix facultatif.
- Date d'événement facultative.
- Date d'expiration facultative.
- Images associées.
- Statut.
- Dates de création, modification, publication, expiration, suspension et suppression logique lorsque pertinentes.

### Messagerie

- Conversations.
- Messages.
- Participants.
- Dates d'envoi et de lecture si la fonctionnalité est retenue.

### Modération et audit

- Signalements.
- Actions de modération.
- Motifs.
- Acteur autorisé.
- Cible.
- Date et résultat.
- Identifiant de corrélation.
- Hachages techniques limités pour certains événements de sécurité compte.

## Minimisation

- Ne pas demander d'adresse postale complète pour le MVP.
- Ne pas stocker de géolocalisation précise si une localisation approximative suffit.
- Ne pas collecter de numéro de téléphone par défaut.
- Ne pas stocker de documents d'identité.
- Ne pas conserver les noms originaux de fichiers comme source de vérité.
- Ne pas utiliser les chemins ou noms fournis par l'utilisateur pour stocker les images.
- Ne pas stocker les jetons temporaires en clair.
- Ne pas exposer l'email d'un membre aux autres membres sans nécessité produit explicite.
- Ne pas stocker les mots de passe en clair ; seuls les hachages Identity sont persistés.
- Ne pas stocker les emails saisis dans les tentatives sensibles lorsqu'un hachage suffit à l'audit.

## Consentement et information

Les membres doivent être informés clairement :

- des données nécessaires à la création de compte ;
- de la visibilité de leurs annonces ;
- de l'usage de la messagerie interne ;
- de l'existence du journal d'audit pour les actions sensibles ;
- des règles de conservation ;
- des moyens de demander export, rectification, suppression ou anonymisation.

Les consentements facultatifs doivent être séparés des conditions nécessaires au fonctionnement du service.

## Visibilité

- Les annonces privées ne doivent pas être accessibles aux visiteurs non authentifiés.
- Les contenus doivent être visibles uniquement dans le périmètre prévu par la communauté.
- Les informations de profil affichées doivent être limitées.
- Les conversations privées ne doivent être visibles que par les participants et les modérateurs autorisés dans les cas prévus.

## Export des données

Un membre doit pouvoir demander un export de ses données personnelles.

L'export devrait couvrir :

- données de compte ;
- profil ;
- annonces créées ;
- images associées lorsque pertinent ;
- conversations et messages selon les règles de confidentialité des autres participants ;
- signalements créés par le membre lorsque cela ne compromet pas la modération ou la sécurité.

L'export doit être produit dans un format exploitable, par exemple JSON et fichiers associés. L'accès à l'export doit être temporaire, authentifié et audité.

## Rectification

Le membre doit pouvoir corriger les données modifiables :

- nom affiché ou pseudonyme ;
- informations de profil ;
- contenu de ses annonces tant qu'elles ne sont pas verrouillées par modération ;
- préférences.

Les changements sensibles peuvent être audités.

## Suppression et anonymisation

La suppression doit être documentée et cohérente avec la sécurité, la modération et les obligations de preuve.

Options prévues :

- suppression logique du compte ;
- anonymisation du compte ;
- suppression des annonces actives ;
- conservation minimale des traces d'audit nécessaires ;
- dissociation des contenus lorsque c'est acceptable ;
- suppression des images non nécessaires.

Les données nécessaires à l'audit ou à la sécurité peuvent être conservées pendant une durée limitée et justifiée. Elles doivent être minimisées.

## Conservation

Les durées exactes devront être validées avant production. Cibles initiales :

- jetons temporaires expirés : suppression rapide après expiration ou tâche de purge ;
- logs techniques : conservation courte ;
- audit log : conservation plus longue, proportionnée au risque ;
- annonces archivées : durée configurable ;
- comptes supprimés : anonymisation ou purge selon politique retenue ;
- images orphelines : purge régulière.

## Mode démo

- Données fictives uniquement.
- Aucun mélange avec les données de production.
- Aucun import de données réelles.
- Emails fictifs ou domaines réservés à la démonstration.
- Réinitialisation possible sans impact production.

## Sous-traitants et services externes

Les services externes devront être documentés avant production :

- hébergement ou machine cible ;
- fournisseur email ;
- GHCR ;
- GitHub Actions ;
- éventuel service de sauvegarde ;
- éventuel reverse proxy ou service TLS.

Pour chaque service, documenter :

- données transmises ;
- finalité ;
- durée de conservation ;
- configuration de sécurité ;
- responsabilité opérationnelle.

## Sauvegardes

Les sauvegardes doivent être protégées au même niveau que la production.

- Accès restreint.
- Chiffrement si disponible.
- Durée de conservation définie.
- Test de restauration.
- Suppression coordonnée avec la politique de rétention.

## Avant production

Avant toute mise en production, il faudra finaliser :

- politique de confidentialité utilisateur ;
- conditions d'utilisation ;
- durées de conservation exactes ;
- procédure d'export ;
- procédure de suppression ;
- registre minimal des traitements ;
- procédure d'incident ;
- mentions relatives aux cookies.
