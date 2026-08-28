# Handoff — MoniPay, POC bout en bout MoMo → carte USD (session du 28/08/2026, après-midi)

Reprise pour un agent frais. Repo : `~/Documents/Projects/moneypay` (branche `main`, tout est
committé et poussé). Contexte design/architecture du prototype : lire `HANDOFF.md` du repo EN
PREMIER, puis les deltas `HANDOFF-2026-08-27.md` et `HANDOFF-2026-08-28*.md`. Le présent
document ne couvre que le POC monétaire réel.

## Ce qui a été construit (voir les commits, ne pas re-décrire)

- `41e9b4e` — POC backend `poc/server.js` (Node sans dépendance, port 8743) :
  recharge MoMo Campay sandbox → solde FCFA → carte virtuelle USD Sudo Africa sandbox.
  Doc complète du flux, des endpoints et des limites : **`poc/README.md`** (source de vérité).
- `f95c1e7` — mode live du prototype (`prototype/`, port 8742) : Recharger + Nouvelle carte
  branchés sur le POC. Fix serveur : writeHead avant await → crash sur erreur handler.
- `0a55a5c` — le parcours d'inscription de la maquette crée le VRAI compte (profil en
  `localStorage.moniProfile`, plus de `?live` dans l'URL). Splash → lock si profil stocké.
- `3dc45d4` — minimum de recharge 25 F en mode live (plafond Campay demo), 1 000 F sinon.

## Décisions fournisseurs (négociées avec l'utilisateur)

- **Cartes USD : Sudo Africa** (app.sudo.africa, sandbox instantané sans KYB).
  Maplerad écarté (dossier KYB exigé dès l'inscription), Bridgecard idem.
- **Collecte MoMo : Campay demo** (demo.campay.net, Cameroun MTN+Orange, API 3 endpoints).
  Notch Pay = plan B accepté ; pawaPay si multi-pays plus tard.
- L'utilisateur a créé les DEUX comptes ; les clés sont dans **`poc/.env`** (gitignoré,
  NE PAS committer, ne jamais afficher les valeurs).

## Pièges API appris en itérant (déjà encodés dans server.js, ne pas re-découvrir)

- Sudo `/cards` exige : `brand`, `debitAccountId` (wallet USD par client, créé via
  `/accounts` avec `accountType:"Current"` + `customerId`), customer avec KYC
  (dob + identity BVN test `22222222222`), `amount` ≥ **3 $**, et un solde wallet >
  amount (frais d'émission → on crédite amount+2 $ via `/accounts/simulator/fund`).
- Le champ `account._id` de la réponse `/cards` diffère du `debitAccountId` envoyé.
- Campay demo : plafond **25 XAF**/collecte, auth par token d'application.
- Réponses Sudo parfois `{statusCode:400}` avec HTTP 200 → le code gère `r.data || r`.

## État au moment du handoff

- Serveur POC lancé en tâche de fond de la session (mode « MoMo: Campay sandbox |
  Carte: Sudo sandbox »). S'il est mort : `node poc/server.js` (le port 8743 doit être
  libre — un vieux `python -m http.server 8743` parasite a été tué cette session).
- Maquette servie par `python3 -m http.server 8742` dans `prototype/` (onglet Chrome ouvert,
  `?v=50` ; après toute modif : rebuild `python3 build.py`, bump `?v=N+1`, hard reload).
- Vérifié en réel : parcours d'inscription complet → compte serveur créé, reprise après
  reload (lock screen), carte Sudo réelle émise depuis la feuille Nouvelle carte
  (masked_pan affiché sur la carte de succès), démo mock `node poc/server.js --demo` verte.
- localStorage de l'onglet remis à zéro : l'utilisateur doit faire SON parcours.

## Prochaine étape immédiate

**Test de la collecte MoMo réelle** : l'utilisateur fait le parcours d'inscription dans la
maquette avec son vrai numéro MTN/Orange, puis Recharger 25 F → push USSD réel à valider
sur son téléphone. Le bouton passe en « Validez sur votre téléphone… » (polling 60 s max).
En cas d'échec : notif in-app avec le message d'erreur Campay ; logs serveur dans le
fichier de sortie de la tâche de fond.

Ensuite (non commencé, dans l'ordre discuté) : rien d'engagé — options évoquées :
webhooks au lieu du polling, persistance serveur, vrai taux de change, simulation d'achat
(dashboard Sudo → Simulator) pour faire vivre la feuille « autorisation temps réel ».

## Style de collaboration (rappel)

Français. Ponytail actif (full) : solution minimale, itérer contre l'API réelle plutôt que
sur-lire les docs. Rituel prototype : `node --check app.js` + `python3 build.py` + vérif
Chrome, commit/push en français sans redemander. Ne pas toucher à la mécanique
`F.dir`/`F.bump` consommés au rendu (voir HANDOFF-2026-08-28.md).

## Compétences suggérées (Skill tool)

- `ponytail:ponytail` — recharger le mode si la session ne l'a pas (le hook l'active normalement).
- `claude-in-chrome` — avant tout pilotage Chrome (vérifs maquette, dashboard Sudo/Campay).
- `design` — UNIQUEMENT si on retouche l'UI de la maquette (nouvelles feuilles/écrans).
- `mattpocock-skills:wizard` — si l'utilisateur doit refaire un parcours de dashboard
  fournisseur (ex. webhooks Campay, funding source Sudo).
