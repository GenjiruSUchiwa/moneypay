# Handoff — Prototype MoniPay (refonte fintech)

## Contexte
Demande d'origine : « utilise /design et refais totalement le design de cette application » — refonte
de MoniPay (ex-MoneyPay), app fintech SwiftUI (le code Swift existe dans le repo : `MoneyPay/`,
`MoneyPay.xcodeproj/`, non suivi par git, PAS renommé MoniPay), livrée sous forme de **prototype
HTML/CSS/JS interactif et autonome** : portefeuille FCFA + recharges MTN MoMo/Orange Money +
cartes virtuelles Visa/Mastercard USD, zone CEMAC (Cameroun). Tout le texte est en **français**.
Exigences : pas de « design slop », pas d'emoji dans l'UI, couleurs/polices cohérentes, vrais logos
de marques en SVG inline, prototype le plus complet possible (~18 écrans/flux).
Historique design : v1 « Billet » (crème/vert sapin/or) REJETÉE par l'utilisateur → v2 clair
Revolut/PayPal en gardant le vert → v3 clair & sombre + animations (état actuel).
L'app est présentée dans un « atelier » : téléphone au centre, rail de navigation des écrans à gauche.

## Références design (apps-modèles & sources)
- **Revolut** = référence principale (l'utilisateur y revient sans cesse) : couleurs v2/v3, CTA vif
  à texte sombre, parcours carte en étapes, picker d'habillages portrait/coverflow/collections.
  ⚠ Il juge les captures Mobbin de Revolut « un peu obsolètes » : préférer SES captures/vidéos
  (voir Emplacements) ; lui en demander pour tout nouveau flux.
- **PayPal** : rôle du vert profond (équivalent de leur navy) dans la v2.
- **Gemini (Google)** : la police — Google Sans Flex (Google Sans Code essayée puis retirée).
- Flux Mobbin consultés (session courante, base du parcours 4 étapes ; ceux de la recherche v1/v2
  n'ont pas été conservés au compactage) :
  - Revolut « Creating a new card » : https://mobbin.com/flows/d64e254a-f416-4b79-a31e-c8ee8d7cb803
  - Revolut « Getting a virtual card » : https://mobbin.com/flows/aca7784d-9a65-4736-906f-3bf1aaf912bf
  - Revolut « Labelling card » : https://mobbin.com/flows/72fa07ba-ca3c-4066-a4aa-1b18d411c413
- Créativité ancrée région plutôt que copie : collection « Héritage » (ndop, bogolan, wax) en
  réponse aux collections artistiques Revolut (Romance/Haunted).

## Emplacements
- **Sources** : `~/Documents/Projects/moneypay/prototype/`
  - `index.html` (coquille + bootstrap thème anti-flash) · `styles.css` (~730 l.) · `app.js` (~2 800 l.)
  - `build.py` → assemble `dist/index.html` (fichier unique, ~210 Ko). Toujours relancer après une modif.
- **Dépôt GitHub** : https://github.com/GenjiruSUchiwa/moneypay (privé, branche `main`).
  Historique : `7892043` (v2 clair) · `6f27e34` (sombre + animations + découpage) · `435dfa2` (MoniPay)
  · `05dd91e` (police unique) · `8b29fe0` (parcours carte 4 étapes + fix course closeSheet)
  · `0e1dc56` (carrousel coverflow + collections Héritage).
- **Artifact publié** (même URL à chaque republication — passer `url`) :
  https://claude.ai/code/artifact/b1804529-24e7-4acc-90d0-e5db23f7bfdf — publier `dist/index.html`.
- **Serveur local** : `python3 -m http.server 8742` tourne dans `prototype/`
  (http://localhost:8742/index.html — incrémenter `?v=N` pour casser le cache).
- **Références utilisateur** : captures Revolut 2026 dans `~/Downloads/IMG_0416..0418.PNG` + vidéo
  `~/Downloads/ScreenRecording_08-27-2026 22-13-42_1.MP4` (frames extraites : `<scratchpad>/fr_*.jpg`,
  planches `sheet_1.jpg`/`sheet_2.jpg`). Il tient à ce qu'on s'appuie sur du Revolut RÉCENT
  (les captures Mobbin sont « obsolètes ») — demander des captures/vidéos au besoin, ffmpeg est dispo.

## Système de design (v3)
- Revolut/PayPal, vert conservé. Clair : fond `#F4F5F7`, encre `#12161C`, action `#067647`,
  vert profond `#053826`, mint `#3CDD9B`. Sombre : fond `#0C0F13`, surfaces `#161B22`,
  action `#2FC988` avec **texte vert profond** sur les CTA.
- **Police unique : Google Sans Flex** (Google Sans Code refusée ; `--mono` = alias de `--sans`).
- **Thème 3 états** : Auto/Clair/Sombre — tokens dupliqués dans DEUX blocs CSS (média + `data-theme`) ;
  toute nouvelle couleur doit exister dans les deux. `localStorage["mp-theme"]`, commutateur rail +
  « Apparence » dans Profil.
- Palette d'analyse : tokens `--cat-*`, sombre validée par le validateur dataviz (L 0,48–0,67).
- Cartes = objets à couleurs fixes. `CARD_THEMES` : 6 « Originales » + 3 « Héritage » à motifs SVG
  (`art:` ndop/bogolan/wax → `cardArt()` ; patterns ton sur ton, marque lisible).

## Architecture JS (vanilla, re-rendu complet)
- `PHASE`/`TAB`+`STACKS`/`SHEET`/`SHEET2`/`F` (+`F_KEEP`), registre `ACTIONS` via `[data-act]`,
  `renderPhone(mode)` ; `"tab"` déclenche cascade d'entrée + compteur de solde.
- Parcours « Nouvelle carte » (`createCardSheet`) en 4 étapes (`F.ccStep`, `F.ccDir`,
  `ccNext/ccBack/ccSkipName`, progression `.seg-progress` — ⚠ `flex:none` en colonne).
  Étape 1 = **carrousel coverflow** : `#cardsel` (scroll-snap, spacers `calc(50% - 108px)`,
  slides 216px), cartes **portrait** via `.cs-rot`/`.cs-tilt` (vcard 289×182 tourné 90°),
  inclinaison JS au scroll (rotateY -46°×ratio + échelle), méta `#cs-name`/`#cs-note` avec fondu
  `.swap`, chips de collections (`ccColl`, `CARD_COLLECTIONS`). Hook d'init dans `renderPhone`
  (après le bloc compteur de solde) — re-brancher si on déplace le rendu.
- `closeSheet`/`closeSheet2` : garde anti-course (rouverture pendant les 270 ms) — conserver.

## Rituel de vérification (après toute modif)
1. `node --check app.js` puis `python3 build.py`.
2. QA Chrome MCP sur l'onglet localhost : piloter en `javascript_tool` (coordonnées peu fiables).
   Helpers : `__norm` (U+00A0/U+202F), `__pick(texte)`, clic rail par `textContent`.
3. Tester clair ET sombre (`[data-theme-set]`), remettre **Auto** à la fin.
4. Republier l'artifact (`dist/index.html` + `url`), commit + push (messages en français).

## Points ouverts
- Proposés, non tranchés : dos de carte visible à mi-rotation pendant les swipes rapides ;
  vraie galerie « toutes les collections » en grille avant le carrousel (cf. écran « Virtual » Revolut).
- Autres feuilles (Recharger, Convertir, Envoyer) encore mono-écran — patron « une décision par
  écran » applicable sur demande.
- Dépôt GitHub toujours nommé `moneypay` ; app Swift (`MoneyPay/`…) non suivie, non renommée.

## Compétences déjà utilisées sur ce projet
- `design` (ui.sh) — invoquée à l'origine du projet, ses règles gouvernent tout le prototype.
- `dataviz` — palettes de catégories clair ET sombre validées avec
  `scripts/validate_palette.js` du skill (jamais à l'œil).
- `artifact-design` — chargée avant la première publication de l'artifact.
- Outils : Mobbin MCP (`search_flows`/`search_screens`, plateforme ios), Chrome MCP (QA),
  ffmpeg (analyse des vidéos de référence). Plugin **ponytail** actif (mode full).

## Compétences suggérées pour la suite (Skill tool)
- `design` — toute nouvelle UI (charger d'abord).
- `dataviz` — toute retouche graphique/palette (valider au script).
- `mobile-app-ui-design` + Mobbin MCP — mais préférer les références FRAÎCHES fournies par
  l'utilisateur (captures/vidéos) ; extraire les vidéos avec ffmpeg (`fps=2` + `tile=6x4`).
- `artifact-design` — seulement pour une NOUVELLE page artifact.
- `swiftui-skills` — si le travail bascule vers le portage du design dans l'app Swift.

## Style de collaboration
Utilisateur francophone, direct sur ce qu'il n'aime pas, envoie des références visuelles — les
analyser AVANT de coder et adapter avec une créativité ancrée (ex. collection « Héritage » ndop/
bogolan/wax plutôt que copie des collections Revolut). Itérations courtes : modifier → vérifier
dans Chrome → republier même URL → commit/push sans redemander. Ponytail actif (diff minimal).
Réponses < 500 mots (CLAUDE.md).
