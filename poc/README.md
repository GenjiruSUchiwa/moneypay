# POC MoniPay — recharge MoMo → carte virtuelle USD

Un seul fichier, zéro dépendance (Node 18+). Chaque fournisseur bascule indépendamment
du mode MOCK (simulé, par défaut) au sandbox réel dès que sa clé est dans `poc/.env` :
Campay demo pour la collecte MTN/Orange (Cameroun), Sudo Africa pour les cartes USD.

## Lancer

```sh
node poc/server.js --demo   # démo auto-vérifiée du flux complet (mock)
node poc/server.js          # serveur sur :8743
```

## Flux complet en curl

```sh
# 1. Inscription (crée aussi le customer Maplerad)
curl -s localhost:8743/signup -d '{"name":"Awa Ndongo","phone":"237670000001","email":"awa@example.com"}'
# → note l'"id" retourné

# 2. Recharge MoMo 20 000 F (Campay : push USSD sur le téléphone en sandbox)
curl -s localhost:8743/topup -d '{"user_id":"<ID>","amount_fcfa":20000}'

# 3. Carte virtuelle USD de 25 $ (débite 15 700 F au taux fixe 628)
curl -s localhost:8743/card -d '{"user_id":"<ID>","amount_usd":25}'

# 4. État
curl -s 'localhost:8743/user?id=<ID>'
```

Rejouer `/card` recharge la carte existante au lieu d'en créer une deuxième.

## Passer en sandbox réel

Créer `poc/.env` :

```
SUDO_API_KEY=...          # https://app.sudo.africa (sandbox) → Developers → API Keys
CAMPAY_APP_USERNAME=...   # https://demo.campay.net → inscription → app → identifiants
CAMPAY_APP_PASSWORD=...
```

Notes sandbox :
- Campay demo plafonne les transactions à **25 XAF** — tester avec `amount_fcfa: 25`.
- Sudo : la carte puise dans la **funding source** par défaut du compte à chaque
  autorisation (pas de fund par carte) ; simuler un achat via Dashboard → Simulator.
- Endpoints Sudo utilisés : `GET /fundingsources`, `POST /customers`, `POST /cards`
  (docs : https://docs.sudo.africa/reference) — ajuster dans `server.js` si besoin.

## Mode live du prototype UI

Le prototype (`prototype/`, servi sur :8742) se branche sur ce serveur via son propre
parcours d'inscription : Bienvenue → « Quel est votre numéro ? » (mettre le VRAI numéro
MTN/Orange) → OTP/code/Face ID (simulés) → « Vos informations » (nom/email modifiables,
défauts OK) → **Continuer** crée le compte réel et stocke le profil en localStorage.
Aux visites suivantes : splash → écran de verrouillage, compte retenu.
- **Recharger** → vraie collecte Campay (push USSD sur le numéro du compte, ≤ 25 F en demo).
- **Nouvelle carte** → vraie carte virtuelle Sudo de 5 $ (minimum 3 $) ; le solde serveur
  est pré-crédité via `/simtopup` (le plafond Campay ne permet pas de financer une carte).
Serveur éteint ou injoignable → la maquette reste 100 % simulée.
Réinitialiser le compte : `localStorage.removeItem("moniProfile")` dans la console.

## Limites assumées du POC

Taux de change codé en dur, état en mémoire (perdu au restart), polling au lieu de
webhooks, pas de KYC. C'est voulu : le POC valide le parcours, pas la prod.
