# MoniPay

MoniPay tient un solde en francs CFA pour un utilisateur de la zone CEMAC, le recharge depuis MTN Mobile Money ou Orange Money, et émet des cartes virtuelles en dollars adossées à ce solde.

Ce fichier fixe la **langue du domaine**. L'interface parle français ; le code, les commentaires et la documentation parlent anglais. Chaque terme donne donc son nom de code entre parenthèses : c'est celui-là, et pas un synonyme, qui nomme un type, une table, un endpoint ou un package. Les règles métier chiffrées (taux, plafonds, arrondis) vivent dans `agents/knowledge-base.md`, pas ici.

## Language

**Utilisateur** (`User`) : personne titulaire d'un compte MoniPay, identifiée par son numéro de téléphone camerounais. Un compte, un solde, une file de cartes. _Éviter_ : client, customer — `customer` désigne l'objet créé chez le fournisseur de cartes, pas la personne chez nous.

**Solde** (`balance`) : montant en FCFA que l'utilisateur peut engager, tenu par le portefeuille et lui seul. Les cartes n'ont pas de solde propre. _Éviter_ : compte, crédit, provision

**Portefeuille** (`Wallet`) : registre du solde en FCFA, des retenues en cours et de la décision d'autorisation. Il est l'unique source de vérité du montant disponible. _Éviter_ : compte, ledger seul

**Disponible** (`available`) : solde moins la somme des retenues en cours. C'est ce montant, jamais le solde brut, qu'une autorisation compare au montant demandé. _Éviter_ : solde réel, solde net

**Recharge** (`top-up`) : entrée d'argent en FCFA dans le portefeuille depuis un compte Mobile Money. Elle est initiée par l'utilisateur et confirmée par le fournisseur, jamais l'inverse. _Éviter_ : dépôt, versement, chargement

**Moyen de recharge** (`TopUpMethod`) : opérateur Mobile Money qui exécute la collecte — MTN MoMo ou Orange Money. _Éviter_ : banque, méthode de paiement

**Collecte** (`collection`) : opération par laquelle le fournisseur MoMo débite le compte de l'utilisateur et crédite celui de MoniPay. Elle commence par une poussée USSD sur le téléphone et se termine par un statut définitif que MoniPay observe, sans jamais le supposer. _Éviter_ : prélèvement, encaissement

**Carte virtuelle** (`VirtualCard`) : carte Visa ou Mastercard en dollars, émise à la demande et adossée au solde FCFA. Elle n'a ni plastique, ni solde propre. _Éviter_ : carte prépayée, carte de débit

**Émission** (`issuing`) : création d'une carte virtuelle chez le fournisseur de cartes. Rejouer une émission recharge la carte existante au lieu d'en créer une deuxième. _Éviter_ : commande, création de compte

**Habillage** (`card theme`) : apparence choisie d'une carte. Purement visuel : il ne change ni les limites, ni le numéro, ni le statut. _Éviter_ : type de carte, catégorie

**Autorisation** (`authorization`) : demande du réseau, au moment d'un achat, de bloquer un montant sur la carte. MoniPay répond immédiatement : acceptée ou refusée. Une autorisation rejouée avec le même identifiant ne bloque pas deux fois. _Éviter_ : paiement, achat, débit

**Retenue** (`hold`) : montant réservé sur le solde par une autorisation acceptée, en attente de son règlement. Elle diminue le disponible sans diminuer le solde. _Éviter_ : blocage, pré-autorisation, réservation

**Règlement** (`settlement`) : arrivée du montant définitif d'une opération, qui transforme la retenue en mouvement réel. Le montant réglé peut différer du montant autorisé. _Éviter_ : validation, confirmation

**Transaction** (`Transaction`) : mouvement inscrit dans l'historique, avec son commerçant, son statut, son montant présenté et son montant réglé. _Éviter_ : opération, ligne, écriture

**Montant présenté** (`presented`) : montant en dollars tel que le commerçant l'a demandé. **Montant réglé** (`settled`) : montant en FCFA effectivement retiré du portefeuille, taux et marge appliqués. Les deux vivent sur la même transaction ; ne jamais n'en afficher qu'un.

**Conversion** (`conversion`) : passage d'un montant en dollars au montant en FCFA correspondant, taux et marge compris. La conversion est arrondie **au franc supérieur** : le sens de l'arrondi est une décision métier, pas un détail de formatage. _Éviter_ : change, calcul du taux

**Taux** (`FXRate`) : nombre de francs CFA pour un dollar, augmenté de la **marge** (`margin`) que MoniPay prend sur chaque conversion. La marge est la source de revenu du produit, pas les frais de carte. _Éviter_ : commission, frais

**Unité mineure** (`minor unit`) : plus petite unité indivisible d'une devise. Le XAF n'en a pas — un franc est une unité mineure — et l'USD en a deux, les cents. Tout montant est stocké en unités mineures, jamais en nombre à virgule flottante. _Éviter_ : centime pour le FCFA, qui n'existe pas

**Plafond** (`limit`) : montant maximum qu'une carte peut dépenser sur une période. Distinguer le plafond mensuel de la carte, le plafond de l'opérateur sur une collecte, et le minimum de recharge : trois contraintes différentes, trois messages différents. _Éviter_ : limite au sens générique

**KYC** (`Kyc`) : vérification de l'identité de l'utilisateur, exigée pour lever les restrictions du compte. Terme conservé tel quel, en français comme en anglais. _Éviter_ : vérification, validation du compte

**Fournisseur** (`provider`) : tiers qui exécute une opération pour MoniPay — Campay pour la collecte MoMo, Sudo Africa pour l'émission de cartes. Un caprice de fournisseur (plafond de bac à sable, corps de réponse enveloppé, code d'erreur maison) est traduit en erreur de domaine à la frontière, et ne remonte jamais tel quel dans une vue. _Éviter_ : partenaire, API

**Bac à sable** (`sandbox`) : environnement de test d'un fournisseur, avec ses propres limites. Ce n'est pas le mode simulé : le bac à sable appelle un vrai service distant. _Éviter_ : mock, démo

**Refus** (`refusal`) : réponse par laquelle MoniPay explique pourquoi une action n'a pas eu lieu, dans les mots de l'utilisateur. Un refus est une réponse attendue, pas une panne. _Éviter_ : erreur, échec

## Component names

Les termes ci-dessus nomment aussi le code, des deux côtés du monorepo :

| Domaine | Module `server/` | Package `ios/` |
|---|---|---|
| Portefeuille, disponible, retenue, autorisation | `MoniPay.Wallet` | `WalletStore` |
| Recharge, collecte, moyen de recharge | `MoniPay.TopUps` | `TopUp` |
| Carte virtuelle, émission, plafond | `MoniPay.Cards` | `Cards` |
| Transaction, montant présenté, montant réglé | `MoniPay.Transactions` | `Transactions` |
| Taux, marge, conversion | `MoniPay.Fx` | `Money` |
| KYC | `MoniPay.Kyc` | `KYC` |
| Utilisateur, session | `MoniPay.Users`, `MoniPay.Sessions` | `Onboarding`, `Settings` |

Un terme qui n'est pas dans cette page n'est pas du domaine : soit il faut l'y ajouter avec sa définition, soit il faut le remplacer par celui qui y est déjà.
