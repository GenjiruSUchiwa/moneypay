---
version: alpha
name: MoniPay
description: Fintech mobile pour la zone CEMAC (Cameroun) — portefeuille FCFA, recharges MTN MoMo / Orange Money, cartes virtuelles Visa/Mastercard USD. Papier froid clair ou encre profonde en sombre, vert d'action franc (#067647 clair / #2FC988 sombre), panneau héro vert profond guilloché, chrome « verre liquide » (réf. iOS 26) sur la barre d'onglets, les feuilles et les menus. Police unique Google Sans Flex, capsules partout, cartes virtuelles à aplats fixes dont une collection « Héritage » à motifs ndop / bogolan / wax. Interface entièrement en français, aucun emoji, logos de marques en SVG inline.

colors:
  paper: "#F4F5F7"
  surface: "#FFFFFF"
  well: "#EAECEF"
  well-2: "#DFE2E7"
  ink: "#12161C"
  ink-2: "#5C6570"
  ink-3: "#9AA1AA"
  green: "#067647"
  green-press: "#05603C"
  green-deep: "#053826"
  on-green: "#FFFFFF"
  deep-ink: "#FFFFFF"
  accent: "#067647"
  accent-bright: "#3CDD9B"
  accent-soft: "rgba(6,118,71,.10)"
  credit: "#0E8345"
  credit-soft: "#DCF3E6"
  debit: "#D92D20"
  debit-soft: "#FEE4E2"
  pend: "#B54708"
  pend-soft: "#FDF0D2"
  hairline: "rgba(18,22,28,.10)"
  rule: "rgba(18,22,28,.17)"
  edge: "rgba(18,22,28,.14)"
  chip-ring: "rgba(18,22,28,.12)"
  dim: "rgba(11,15,20,.42)"
  glass: "rgba(255,255,255,.62)"
  glass-menu: "rgba(250,251,252,.80)"
  glass-btn: "rgba(255,255,255,.55)"
  net-visa: "#1434CB"
  cat-streaming: "#2a78d6"
  cat-shopping: "#eb6834"
  cat-software: "#1baf7a"
  cat-food: "#eda100"
  cat-transport: "#e87ba4"
  cat-travel: "#008300"
  cat-ads: "#4a3aa7"
  cat-other: "#87929E"

colors-dark:
  paper: "#0C0F13"
  surface: "#161B22"
  well: "#1F2630"
  well-2: "#2A3340"
  ink: "#F0F3F6"
  ink-2: "#9CA5B0"
  ink-3: "#67707C"
  green: "#2FC988"
  green-press: "#28B579"
  green-deep: "#0A4331"
  on-green: "#04301F"
  accent: "#3AD695"
  accent-soft: "rgba(58,214,149,.13)"
  credit: "#4CD394"
  credit-soft: "rgba(23,178,106,.16)"
  debit: "#F97066"
  debit-soft: "rgba(240,68,56,.15)"
  pend: "#EEA23D"
  pend-soft: "rgba(234,141,42,.15)"
  hairline: "rgba(240,243,246,.08)"
  rule: "rgba(240,243,246,.16)"
  edge: "rgba(240,243,246,.15)"
  dim: "rgba(0,0,0,.6)"
  glass: "rgba(26,32,40,.58)"
  glass-menu: "rgba(20,25,31,.74)"
  glass-btn: "rgba(240,243,246,.08)"
  net-visa: "#F0F3F6"
  cat-streaming: "#3280de"
  cat-shopping: "#d25117"
  cat-software: "#039868"
  cat-food: "#ab7301"
  cat-transport: "#c05781"
  cat-travel: "#2b9927"
  cat-ads: "#766ddf"
  cat-other: "#7d8793"

typography:
  display-amount:
    fontFamily: "Google Sans Flex, sans-serif"
    fontSize: 44px
    fontWeight: 600
    lineHeight: 1.1
    letterSpacing: -0.02em
  display-hero:
    fontFamily: "Google Sans Flex, sans-serif"
    fontSize: 40px
    fontWeight: 600
    lineHeight: 1.1
    letterSpacing: -0.02em
  title-page:
    fontFamily: "Google Sans Flex, sans-serif"
    fontSize: 28px
    fontWeight: 600
    lineHeight: 1.2
    letterSpacing: -0.02em
  title-lg:
    fontFamily: "Google Sans Flex, sans-serif"
    fontSize: 24px
    fontWeight: 600
    lineHeight: 1.2
    letterSpacing: -0.02em
  title-section:
    fontFamily: "Google Sans Flex, sans-serif"
    fontSize: 19px
    fontWeight: 600
    lineHeight: 1.3
    letterSpacing: -0.015em
  nav-title:
    fontFamily: "Google Sans Flex, sans-serif"
    fontSize: 16px
    fontWeight: 600
    lineHeight: 1.3
    letterSpacing: -0.01em
  body-md:
    fontFamily: "Google Sans Flex, sans-serif"
    fontSize: 15px
    fontWeight: 400
    lineHeight: 1.4
    letterSpacing: 0
  body-strong:
    fontFamily: "Google Sans Flex, sans-serif"
    fontSize: 15px
    fontWeight: 500
    lineHeight: 1.4
    letterSpacing: 0
  body-sm:
    fontFamily: "Google Sans Flex, sans-serif"
    fontSize: 13.5px
    fontWeight: 400
    lineHeight: 1.5
    letterSpacing: 0
  caption:
    fontFamily: "Google Sans Flex, sans-serif"
    fontSize: 12px
    fontWeight: 400
    lineHeight: 1.55
    letterSpacing: 0
  eyebrow:
    fontFamily: "Google Sans Flex, sans-serif"
    fontSize: 13px
    fontWeight: 500
    lineHeight: 1.4
    letterSpacing: 0
    textTransform: none
  button:
    fontFamily: "Google Sans Flex, sans-serif"
    fontSize: 16px
    fontWeight: 500
    lineHeight: 1
    letterSpacing: -0.005em
  button-small:
    fontFamily: "Google Sans Flex, sans-serif"
    fontSize: 13.5px
    fontWeight: 500
    lineHeight: 1
    letterSpacing: 0
  chip:
    fontFamily: "Google Sans Flex, sans-serif"
    fontSize: 13px
    fontWeight: 500
    lineHeight: 1
    letterSpacing: 0
  keypad:
    fontFamily: "Google Sans Flex, sans-serif"
    fontSize: 23px
    fontWeight: 500
    lineHeight: 1
    letterSpacing: 0

rounded:
  tile: 12px
  control: 14px
  card: 16px
  panel: 24px
  float: 30px
  pill: 999px

spacing:
  xs: 8px
  sm: 12px
  md: 16px
  gutter: 20px
  lg: 24px
  section: 26px
  tab-clearance: 104px

components:
  button-primary:
    backgroundColor: "{colors.green}"
    textColor: "{colors.on-green}"
    typography: "{typography.button}"
    rounded: "{rounded.pill}"
    height: 52px
  button-primary-active:
    backgroundColor: "{colors.green-press}"
    textColor: "{colors.on-green}"
    transform: scale(.985)
  button-primary-disabled:
    opacity: 0.35
    pointerEvents: none
  button-quiet:
    backgroundColor: "{colors.well}"
    textColor: "{colors.ink}"
    typography: "{typography.button}"
    rounded: "{rounded.pill}"
    height: 52px
  button-ghost:
    backgroundColor: transparent
    textColor: "{colors.ink-2}"
    typography: "{typography.button}"
  button-danger:
    backgroundColor: "{colors.debit-soft}"
    textColor: "{colors.debit}"
    typography: "{typography.button}"
    rounded: "{rounded.pill}"
    height: 52px
  button-small:
    typography: "{typography.button-small}"
    rounded: "{rounded.pill}"
    height: 36px
    padding: 0 14px
  chip:
    backgroundColor: "{colors.well}"
    textColor: "{colors.ink-2}"
    typography: "{typography.chip}"
    rounded: "{rounded.pill}"
    height: 34px
    padding: 0 13px
  chip-active:
    backgroundColor: "{colors.green}"
    textColor: "{colors.on-green}"
  segments-glass:
    backgroundColor: "{colors.glass-btn}"
    backdropFilter: "blur(26px) saturate(1.8)"
    rounded: "{rounded.pill}"
    padding: 3px
  segment-active:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.ink}"
    rounded: "{rounded.pill}"
    height: 30px
  pill-credit:
    backgroundColor: "{colors.credit-soft}"
    textColor: "{colors.credit}"
    rounded: "{rounded.pill}"
    height: 20px
  pill-debit:
    backgroundColor: "{colors.debit-soft}"
    textColor: "{colors.debit}"
    rounded: "{rounded.pill}"
    height: 20px
  pill-pending:
    backgroundColor: "{colors.pend-soft}"
    textColor: "{colors.pend}"
    rounded: "{rounded.pill}"
    height: 20px
  toggle:
    backgroundColor: "{colors.well-2}"
    size: 46px x 28px
    rounded: "{rounded.pill}"
  toggle-on:
    backgroundColor: "{colors.green}"
  nav-button-glass:
    backgroundColor: "{colors.glass-btn}"
    backdropFilter: "blur(26px) saturate(1.8)"
    size: 38px
    rounded: "{rounded.pill}"
  tab-bar-glass:
    backgroundColor: "{colors.glass}"
    backdropFilter: "blur(26px) saturate(1.8)"
    rounded: "{rounded.pill}"
    padding: 5px
  tab-item-active:
    textColor: "{colors.green}"
    backgroundColor: "color-mix(in srgb, {colors.green} 14%, transparent)"
  widget-trigger:
    backgroundColor: "{colors.glass}"
    size: 54px
    rounded: "{rounded.pill}"
  widget-menu:
    backgroundColor: "{colors.glass-menu}"
    rounded: "{rounded.float}"
    grid: "2 x 84px"
  sheet:
    backgroundColor: "{colors.glass-menu}"
    backdropFilter: "blur(26px) saturate(1.8)"
    rounded: "{rounded.float} {rounded.float} 0 0"
    topInset: 14px
  action-sheet:
    backgroundColor: "{colors.glass-menu}"
    rounded: "{rounded.float}"
    inset: 10px
  toast:
    backgroundColor: "{colors.glass-menu}"
    textColor: "{colors.ink}"
    rounded: "{rounded.pill}"
  field:
    backgroundColor: "{colors.well}"
    textColor: "{colors.ink}"
    typography: "{typography.body-md}"
    rounded: "{rounded.control}"
    height: 50px
    padding: 0 15px
  field-focused:
    backgroundColor: "{colors.surface}"
    boxShadow: "inset 0 0 0 1.5px {colors.green}"
  keypad-key:
    typography: "{typography.keypad}"
    height: 52px
    rounded: "{rounded.tile}"
  row:
    minHeight: 56px
    padding: 13px 0
  icon-tile:
    backgroundColor: "{colors.well}"
    textColor: "{colors.ink-2}"
    size: 38px
    rounded: "{rounded.pill}"
  logo-tile:
    backgroundColor: "#FFFFFF"
    boxShadow: "inset 0 0 0 1px {colors.chip-ring}"
    size: 40px
    rounded: "{rounded.tile}"
  hero-panel:
    backgroundColor: "{colors.green-deep}"
    textColor: "{colors.deep-ink}"
    rounded: "{rounded.panel}"
    padding: 22px 22px 18px
    margin: 6px {spacing.gutter} 0
  quick-action:
    backgroundColor: "{colors.well}"
    size: 52px
    rounded: "{rounded.pill}"
  method-card:
    backgroundColor: "{colors.well}"
    rounded: 20px
    padding: 13px 16px
  fx-card:
    backgroundColor: "{colors.well}"
    rounded: 20px
    padding: 15px 16px
  fx-mid:
    backgroundColor: "{colors.green}"
    textColor: "{colors.on-green}"
    size: 31px
    rounded: "{rounded.pill}"
  vcard:
    aspectRatio: 1.586
    rounded: "{rounded.card}"
    boxShadow: "inset 0 0 0 1px rgba(255,255,255,.09), 0 10px 28px -14px rgba(12,17,23,.4)"
  meter:
    backgroundColor: "{colors.well-2}"
    fillColor: "{colors.green}"
    height: 4px
    rounded: "{rounded.pill}"
  limit-ring:
    trackColor: "{colors.well-2}"
    fillColor: "{colors.green}"
    warnColor: "{colors.debit}"
    size: 58px
    strokeWidth: 5.5px
  card-detail-panel:
    backgroundColor: "{colors.paper}"
    rounded: "{rounded.float} {rounded.float} 0 0"
  success-mark:
    backgroundColor: "{colors.credit}"
    textColor: "#FFFFFF"
    size: 58px
    rounded: "{rounded.pill}"
  fail-mark:
    backgroundColor: "{colors.debit}"
    textColor: "#FFFFFF"
    size: 58px
    rounded: "{rounded.pill}"
  alert-card:
    backgroundColor: "{colors.debit-soft}"
    rounded: "{rounded.card}"
    padding: 14px
---

# MoniPay — DESIGN.md

Source de vérité : **`prototype/styles.css`** (tokens et composants) et **`prototype/app.js`**
(`CARD_THEMES`, `CARD_COLLECTIONS`, `CATEGORIES`, `KIND_LABELS`, `STATUS_META`, `cardArt()`).
Catalogues visuels : **`preview.html`** (clair) et **`preview-dark.html`** (sombre).

> L'app SwiftUI (`MoneyPay/`) porte un système antérieur et divergent. **Elle ne fait pas foi.**

## Overview

MoniPay est une fintech mobile pour le Cameroun (zone CEMAC) : portefeuille FCFA, recharges
MTN MoMo / Orange Money, cartes virtuelles Visa/Mastercard USD. Références : **Revolut**
(principale — parcours carte, CTA, carrousels), **PayPal** (rôle du vert profond), **Wise**
(chronologie de succès), **iOS 26 / Liquid Glass** (barre d'onglets, feuilles, menu widget).

L'atmosphère est un **papier froid** (`{colors.paper}` — #F4F5F7) en clair, une **encre
profonde** (#0C0F13) en sombre. Trois registres de surface rythment l'app :

1. **Papier** (`{colors.paper}`) — le fond de tous les écrans.
2. **Vert profond** (`{colors.green-deep}`) — le panneau héro du solde et les pastilles
   d'icône des menus. Un panneau, jamais un thème de page (exception : `.layer.dark-bg`
   pour les écrans immersifs d'onboarding).
3. **Verre liquide** (`{colors.glass}` + flou) — le chrome flottant : barre d'onglets,
   boutons de navigation, feuilles, menus, toasts.

**Caractéristiques clés :**
- Le vert `{colors.green}` est **l'action**, pas le décor : CTA, chip actif, toggle, jour
  sélectionné. Jamais en fond de page.
- En sombre, le vert devient vif (#2FC988) et son texte devient **vert profond**
  (#04301F) — jamais blanc sur vert vif.
- Police **unique** : Google Sans Flex. Les rôles « techniques » (PAN, références, ticks)
  se distinguent par la chasse élargie et les chiffres tabulaires, pas par une seconde
  famille. Les capitales sont réservées aux **marquages gravés de la carte**
  (VIRTUELLE, EXPIRE, CVV) — jamais aux libellés de section.
- Boutons, chips, pills, segments, barre d'onglets : **capsules** (999px). Les rayons
  rectangulaires sont réservés aux surfaces (tuiles 12 → surfaces flottantes 30).
- Motif **guilloché « rosace »** (cercles concentriques SVG, opacité ~5 %) sur l'atelier,
  le héro et les cartes — la signature « billet de banque » du produit.
- Cartes virtuelles à **aplats fixes** (insensibles au thème), dont la collection
  **« Héritage »** : motifs ndop, bogolan, wax — créativité ancrée région, pas une copie
  des collections Revolut.
- Interface en **français**, séparateur de milliers **U+202F**, aucun emoji, logos de
  marques (MTN, Orange, Visa, Mastercard…) en **SVG inline**.

## Colors

> **RÈGLE CRITIQUE — thème.** Les tokens existent dans **deux** blocs CSS :
> `@media (prefers-color-scheme: dark)` **et** `:root[data-theme="dark"]`. Toute nouvelle
> couleur doit être définie dans **les deux**, sinon l'un des deux modes casse en silence.
> Le frontmatter `colors:` donne le clair, `colors-dark:` le sombre (extension locale).
> Le sombre n'est **pas une inversion** : chaque rôle est reconstruit.

### Surfaces & encre
- **Papier** (`{colors.paper}` — #F4F5F7 · sombre #0C0F13) : fond de page unique.
- **Surface** (`{colors.surface}` — #FFFFFF · #161B22) : segment actif, champ focalisé,
  option sélectionnée — la surface « soulevée ».
- **Creux** (`{colors.well}` — #EAECEF · #1F2630) : champs, pastilles d'icône, chips,
  cartes de méthode et de change. Le fond « en retrait ».
- **Creux 2** (`{colors.well-2}` — #DFE2E7 · #2A3340) : rails de jauge, points inactifs,
  fond du toggle.
- **Encre** (`{colors.ink}` / `{colors.ink-2}` / `{colors.ink-3}`) : texte principal /
  secondaire (dont les eyebrows) / récessif (chevrons, placeholders).

### Vert — action & identité
- **Vert** (`{colors.green}` — #067647 · #2FC988) : l'aplat d'action. CTA primaire, chip
  actif, toggle actif, remplissage de jauge, curseur de saisie.
- **Sur vert** (`{colors.on-green}` — #FFFFFF · **#04301F**) : texte sur l'aplat. Le
  passage au texte sombre en mode sombre est une signature du système.
- **Vert pressé** (`{colors.green-press}`) : état actif du CTA.
- **Vert profond** (`{colors.green-deep}` — #053826 · #0A4331) : panneau héro, pastilles
  d'icône des menus d'action, écrans immersifs. Son texte est `{colors.deep-ink}`
  (#FFFFFF, constant).
- **Accent** (`{colors.accent}`) : liens, `.sh-link`, curseur `.fx-caret`.
- **Accent vif** (`{colors.accent-bright}` — #3CDD9B) : accents **sur** vert profond
  uniquement (eyebrow du héro, icônes des menus). Jamais sur papier.

### Sémantique monétaire
- **Crédit** (`{colors.credit}` / `{colors.credit-soft}`) : entrées d'argent, statut
  « Réussi », baisse de dépenses.
- **Débit** (`{colors.debit}` / `{colors.debit-soft}`) : sorties, statut « Refusé »,
  destructif, jauge en alerte.
- **Attente** (`{colors.pend}` / `{colors.pend-soft}`) : « En attente », hausse de
  dépenses, badge de filtres.
- En sombre, les fonds `-soft` deviennent des **rgba translucides** (ex.
  `rgba(23,178,106,.16)`), pas des pastels opaques.
- **Ces trois couleurs ne servent jamais de teinte de série** dans un graphique.

### Filets & structure
- **Hairline** (`{colors.hairline}`) : séparateurs de lignes. **Rule** (`{colors.rule}`) :
  filets appuyés, poignées de feuille. **Edge** (`{colors.edge}`) : liserés de cartes et
  nuanciers. **Chip-ring** (`{colors.chip-ring}`) : liseré des pastilles de logo (fond
  blanc **constant**, même en sombre). **Dim** (`{colors.dim}`) : voile modal.
  Tous en opacité, jamais en gris opaque.

### Verre liquide
- `{colors.glass}` (barre d'onglets, déclencheur widget), `{colors.glass-menu}` (feuilles,
  menus, toast), `{colors.glass-btn}` (boutons de nav, segments), avec
  `--glass-blur: blur(26px) saturate(1.8)` et `--glass-edge` (double liseré intérieur).
- **Un élément de verre porte toujours les trois** : fond translucide + flou + liseré.
  Sans liseré, le verre lit comme un simple fond délavé.

### Données — palette catégorielle
8 tokens `--cat-*` (voir frontmatter), **validés au script** (`validate_palette.js` du
skill dataviz — bande de clarté, plancher de chroma, séparation CVD), jamais à l'œil.
Divertissement `cat-streaming` · Achats `cat-shopping` · Logiciels `cat-software` ·
Restauration `cat-food` · Transport `cat-transport` · Voyage `cat-travel` · Publicité
`cat-ads` · Autre `cat-other` (jamais une identité, toujours étiqueté).
`AV_TINTS` réutilise 5 de ces tokens pour les avatars de contacts (couleur stable par index).

## Typography

### Font Family
**Google Sans Flex** est la seule famille (Google Fonts, `wght 300..800`). Les rôles
techniques (PAN, références, ticks d'axe) gardent la même famille et se distinguent par
`letter-spacing` élargi + `tabular-nums`. L'ancien alias `--mono` a été **supprimé**
(Google Sans Code essayée puis retirée). Fallback :
`-apple-system, BlinkMacSystemFont, "Segoe UI", system-ui, sans-serif`.

### Hierarchy

| Token | Taille | Poids | Chasse | Usage |
|---|---|---|---|---|
| `{typography.display-amount}` | 44px | 600 | -0.02em | saisie de montant (`.ae-value`) |
| `{typography.display-hero}` | 40px | 600 | -0.02em | solde du héro |
| `{typography.title-page}` | 28px | 600 | -0.02em | titre d'écran (`.t-page`, `text-wrap:balance`) |
| `{typography.title-lg}` | 24px | 600 | -0.02em | titre secondaire (`.t-title`) |
| `{typography.title-section}` | 19px | 600 | -0.015em | titre de section, titre de feuille |
| `{typography.nav-title}` | 16px | 600 | -0.01em | titre de barre de nav |
| `{typography.body-md}` | 15px | 400 | 0 | corps courant, titres de ligne |
| `{typography.body-strong}` | 15px | 500 | 0 | libellés appuyés |
| `{typography.body-sm}` | 13.5px | 400 | 0 | sous-titres, notes |
| `{typography.caption}` | 12px | 400 | 0 | micro-notes, fine-print |
| `{typography.eyebrow}` | 13px | 500 | 0 | libellé de section, sentence case (`{colors.ink-2}`) |
| `{typography.button}` | 16px | 500 | -0.005em | CTA |
| `{typography.keypad}` | 23px | 500 | tabular | touches du pavé |

### Montant composé — `.money`
Devise en préfixe à 0.62em (`.m-cur`), **centimes en exposant** à 0.55em remontés de
0.52em (`.m-frac`), unité FCFA en suffixe à 0.4em en `{colors.ink-2}` (`.m-unit`).
Séparateur de milliers : **espace fine insécable U+202F**. `tabular-nums` partout où des
nombres s'alignent (`.tnum`).

```html
<span class="money display"><span class="m-cur">$</span>125<span class="m-frac">50</span></span>
<span class="money display">78 500<span class="m-unit">FCFA</span></span>
```

### Principles
Poids 600 maximum pour les titres (jamais 700+). Chasse négative croissante avec la
taille (-0.005em → -0.02em). **Aucune capitale dans l'UI** — seules exceptions : les
marquages gravés de la carte (`.vc-virtual`, `.s-l`), à la manière d'une carte physique.
`text-wrap: pretty` sur les paragraphes, `balance` sur les titres.
⚠ QA : normaliser U+00A0 / U+202F avant toute comparaison de chaînes.

## Layout

### Appareil & structure
- Écran de référence : **393 × 852** (iPhone 15 Pro) dans un « atelier » de présentation
  (rail des écrans à gauche, téléphone au centre, rosace guillochée au coin).
- Structure : `.app` → `.layer` (fond papier) → `.statusbar` 54px → `.navbar` 52px →
  `.scroll` → barre d'onglets flottante.
- **Gutter unique** : `{spacing.gutter}` (20px) via la classe `.gutter` — jamais un
  padding horizontal en dur.
- La barre d'onglets flotte à 24px du bas ; le contenu défile dessous grâce à
  `padding-bottom: {spacing.tab-clearance}` (104px).

### Spacing
`{spacing.xs}` 8 · `{spacing.sm}` 12 · `{spacing.md}` 16 · `{spacing.gutter}` 20 ·
`{spacing.lg}` 24 · `{spacing.section}` 26 (padding-top d'un `.section-head`, +12 dessous).
Lignes de liste : 13px vertical, min 56px. Feuilles : inset haut 14px.

### Listes sans conteneurs
Les listes sont posées sur le papier et séparées par `.rule.inset`
(retrait `calc(gutter + 52px)` = aligné après la pastille). Pas de cartes blanches
autour des groupes de lignes ; les seuls « conteneurs » sont les creux fonctionnels
(`.method-card`, `.fx-card`) et le héro.

## Depth & Elevation

| Niveau | Traitement | Usage |
|---|---|---|
| Plat | fond papier, aucun relief | écrans, listes |
| Creux | `{colors.well}`, aucune ombre | champs, chips, tuiles, cartes de flux |
| Soulevé | `{colors.surface}` + `--shadow-1` | segment actif, option sélectionnée |
| Panneau | `{colors.green-deep}` + rosace | héro du solde |
| Verre | glass + blur 26px + `--glass-edge` + ombre portée | tabbar, nav, feuilles, menus, toast |
| Objet | aplat fixe + liseré + `0 10px 28px -14px` | carte virtuelle |

- Ombres : `--shadow-1` (1px), `--shadow-pop` (menus, 40px), `--shadow-sheet` (feuilles,
  vers le haut). En sombre, elles passent en noir profond (alpha .5–.55).
- **Pas de verre sur verre** : dans une feuille (déjà en verre), les contrôles
  redeviennent des aplats `{colors.well}` — règle codée en CSS
  (`.sheet .nb-btn, .sheet .segments`).
- Le voile `{colors.dim}` s'accompagne d'un `backdrop-filter: blur(12px)` progressif.

## Shapes

| Token | Valeur | Usage |
|---|---|---|
| `{rounded.tile}` | 12px | pastilles de logo, touches du pavé, jours du calendrier |
| `{rounded.control}` | 14px | champs, options réseau |
| `{rounded.card}` | 16px | carte virtuelle, cartes d'alerte |
| `{rounded.panel}` | 24px | héro du solde (carte posée dans la page) |
| `{rounded.float}` | 30px | surfaces flottantes : feuilles modales (haut), panneau d'action, menu widget, panneau du détail de carte (`--r-float`) |
| `{rounded.pill}` | 999px | boutons, chips, pills, segments, tabbar, toggles, avatars ronds |
| 20px | — | `.method-card`, `.fx-card` (hors barème, assumé) |

- Carte virtuelle : **ratio 1.586** (ISO/IEC 7810 ID-1), non négociable.
- Guilloché « rosace » : data-URI SVG, encre `#14251E` à 5,5 % en clair, `#3CDD9B` à 5 %
  en sombre. Coin bas-droit de l'atelier, coin haut-droit du héro (dérive lente 30s),
  coin bas-droit des cartes.
- Illustrations : pas de photo. Logos de marques SVG inline dans `.logo-tile` (fond blanc
  constant + `{colors.chip-ring}`).

## Components

### Navigation
**`navbar`** — 52px, sans fond. Boutons `nav-button-glass` (38px, capsule de verre),
titre centré en `{typography.nav-title}` (absolu, ellipse), `.nb-text` pour les actions
textuelles (« Annuler », `.strong` pour l'action d'engagement).

**`tab-bar-glass`** — capsule de verre flottante (`left/right:14px, bottom:24px`), 5px de
padding interne. Onglet actif : icône + libellé en `{colors.green}` sur fond
`color-mix(green 14%)`, rebond `tabPop` 0,32s. À sa droite, **`widget-trigger`** (54px)
déplie **`widget-menu`** : grille 2×84px de tuiles (`.wm-ico` 58px, coins 19px),
`transform-origin: 88% 100%`, scrim flouté.

**`stack-view`** — poussée de pile : entrant `translateX(100%)→0`, sous-jacent recule de
26%, 260ms `cubic-bezier(.32,.72,.28,1.02)`.

### Boutons
**`button-primary`** — capsule 52px, `{colors.green}` / `{colors.on-green}`,
`{typography.button}`. Actif : `{colors.green-press}` + `scale(.985)`. Désactivé :
opacité .35. Spinner : anneau 16px `currentColor`.
**`button-quiet`** (`{colors.well}`) · **`button-ghost`** (transparent, `{colors.ink-2}`) ·
**`button-danger`** (`{colors.debit-soft}` / `{colors.debit}`) · **`button-small`**
(36px, largeur au contenu).

### Sélection & états
**`chip`** — capsule 34px, `{colors.well}` ; active = `{colors.green}` +
`{colors.on-green}`. **`segments-glass`** — conteneur capsule en verre, segment actif sur
`{colors.surface}` + `--shadow-1`. **`pill-credit|debit|pending|neutral|accent`** —
capsule 20px, **toujours icône + libellé**. **`toggle`** — 46×28, pastille blanche,
ressort `cubic-bezier(.3,.7,.3,1.2)`. **`meter`** — jauge 4px, remplissage
`{colors.green}` (`.warn` → `{colors.debit}`). **`success-mark`** / **`fail-mark`** —
disque 58px, tracé du trait 0,4s + onde `ringOut` 0,7s.

### Lignes & données clé-valeur
**`row`** — min 56px, `:active{opacity:.6}` (`.static` pour les lignes inertes).
Pastille (`icon-tile` rond 38px `{colors.well}`, ou `logo-tile` carré 40px blanc) +
`.r-title` 15px / `.r-sub` 13px + `.r-value` / chevron. `.destructive` passe le titre
en `{colors.debit}`. Séparées par `.rule.inset`.
**`kv`** — clé `{colors.ink-2}` à gauche, valeur tabulaire à droite (`.strong`, `.mono`,
`.copyable`). **`section-head`** — `{typography.title-section}` + compteur `.sh-count` +
lien `.sh-link` en `{colors.accent}` ou chevron.

### Saisie
**`field`** — 50px, `{colors.well}` ; focus → `{colors.surface}` + liseré 1,5px
`{colors.green}`. **`keypad`** — grille 3 colonnes, touches 52px `{typography.keypad}`,
appui = fond `{colors.well}` (variante `.on-green` sur vert profond).
**`amount-entry`** — montant centré 44px (`.empty` en `{colors.ink-3}`), sous-ligne de
conversion (`.warn` si solde insuffisant), pulsation `digitPop` à chaque frappe.
**`otp-boxes`** — cases 56px, curseur = liseré vert. **`pass-dots`** — 13px, secousse
`shake` 0,4s à l'erreur.

### Surfaces modales
**`sheet`** — plein écran moins 14px, verre, coins `{rounded.float}`, entrée 380ms
`cubic-bezier(.26,1.06,.34,1)`, poignée `.sheet-grab`. Variante `.fit` à hauteur de
contenu (calendrier, filtres). **`action-sheet`** — panneau de verre ancré à 10px des
bords, tuiles 44px sur `{colors.green-deep}` avec icône `{colors.accent-bright}`
(`.destructive` → rouge). **`toast`** — capsule de verre centrée à 110px du bas.

### Écrans monétaires
**`hero-panel`** — panneau `{colors.green-deep}`, coins 24px, rosace en dérive lente,
eyebrow `{colors.accent-bright}` + œil de masquage, solde 40px en `.money`, équivalent
USD, rangée de `quick-action` (disques 52px en verre translucide blanc — variante
`.on-green`).
**`fx-stack`** — pile Convertir : deux `fx-card` (drapeau rond `.fl-flag`, code devise,
montant 23px — `.credit` pour le reçu, `.warn` si insuffisant, `.dim` si vide), pastille
**`fx-mid`** (31px, `{colors.green}`) posée sur la couture, curseur clignotant
`.fx-caret`, chip du taux effectif (« 1 $ = 628 F, marge incluse »).
**`method-card`** — méthode de recharge : logo + nom + « Changer », fond `{colors.well}`.
**`recents`** — rangée d'avatars teintés `AV_TINTS` + prénom (Envoyer).
**`tl` (chronologie Wise)** — jalons en cascade (disques `{colors.credit}` à 14 % sur
fil `{colors.rule}`), délais .35/.55/.75s, sur l'écran de succès d'un envoi.
**`month-strip`** — entrées/sorties du mois, cellules séparées par un hairline.

### Carte virtuelle
**`vcard`** — ratio 1.586, coins `{rounded.card}`, liseré intérieur + ombre portée.
Contenu : marque + « MoniPay », marquage `VIRTUELLE` gravé en capitales, libellé, PAN
masqué (`•• 1234`, chasse .1em), marque réseau (h 26px, compact 18px), rosace au coin.
Habillages clairs : `.light-art` (liseré `{colors.edge}`, ombre allégée).
- **Révélation sur la face** (réf. `contentTransition(.numericText())` SwiftUI) : pas de
  flip — le PAN complet et les marquages `EXPIRE` / `CVV` apparaissent **sur** la carte,
  animés par `panRoll` (0,45s : montée + flou qui se dissipe, secrets décalés de 0,08s).
- **Gel** : voile glacé (reflets radiaux bleutés + liseré givré interne) +
  `blur(7px) saturate(.55) brightness(1.12)`, **givre SVG `.vf-ice`** (fougères de glace
  aux coins + cristaux épars) qui pousse en cascade (`iceGrow` .1/.28/.5s), puis `popIn`
  du disque flocon.

**Détail de carte — scène ambiante (réf. Revolut) :**
- **`cd-ambient`** : dégradé plein haut d'écran teinté par l'aplat de la carte —
  `--cd-tint` (carte gelée → ardoise #5D6F7E), dosé par `--cd-mix` (24 % clair / 32 %
  sombre) et `--cd-mix2`, fondu vers `{colors.paper}`. L'ombre de la carte héro reprend
  la teinte (`color-mix` 50 %).
- **`cd-panel`** (`{components.card-detail-panel}`) : le contenu monte sur la scène dans
  un panneau `{colors.paper}` à coins `{rounded.float}`. Ordre : actions rapides →
  détails repliables → dépense + anneau → transactions → réglages.
- **`limit-ring`** (`.cd-ring`) : anneau de plafond 58px, trait 5,5, se dessine en 0,9s
  à l'arrivée ; `> 85 %` → `{colors.debit}` ; % centré ; tap → contrôles.
- **Détails repliables** (`.cd-collapse`) : dépliage `grid-template-rows 0fr→1fr` 0,42s
  — le contenu dessous est **poussé en douceur**, jamais brutalement ; le repli est animé
  AVANT le re-rendu (garde `F._cdBusy`).
- **Défilement** : parallaxe + fondu du héros (`translateY ×.38`, scale ≥ .86), titre de
  navbar qui apparaît après 170px avec voile de verre sous barre d'état + navbar.
- Sous la carte : pilule d'état seule (Active / Gelée) — pas de rappel réseau/PAN.
- **Sélecteur (assistant 4 étapes)** : carrousel **coverflow portrait** — carte 289×182
  tournée de 90°, slides 216px, scroll-snap, inclinaison JS `rotateY(-46°×ratio)`,
  perspective 1100px, méta en fondu, chips de collections.
- **Onglet Cartes** : carrousel une-carte (slides 258px), points `.cw-dots` (actif étiré
  16px), méta synchronisée, `.cw-ghost` pointillé « ajouter », état vide en éventail
  `.fan` (3 cartes ±13°).

**`CARD_THEMES`** — aplats **fixes** (insensibles au thème) :

| Clé | Aplat | Encre | Libellé | Collection |
|---|---|---|---|---|
| `sapin` | #053826 | #FFFFFF | Sapin | Originales |
| `encre` | #15181B | #F2F3F1 | Encre | Originales |
| `ivoire` | #E7E9EC | #171C22 | Argent (`light:true`) | Originales |
| `terre` | #9E4A2E | #F7EDE4 | Terre | Originales |
| `cobalt` | #1E3C72 | #EAEFF8 | Cobalt | Originales |
| `ardoise` | #44525A | #EEF2F1 | Ardoise | Originales |
| `ndop` | #23307C | #FFFFFF | Ndop | Héritage |
| `bogolan` | #26190E | #F0E4CE | Bogolan | Héritage |
| `wax` | #0D5F52 | #FFF6E4 | Wax | Héritage |

Les thèmes Héritage portent un motif SVG plein cadre **ton sur ton** (`cardArt(kind)` :
pattern répété, opacité .16–.22 — losanges ndop, chevrons pointés bogolan, cercles
concentriques wax). La marque reste lisible par-dessus.
**`card-swatch`** — nuancier 42×30, sélection = anneau encre à -5px. **`net-option`** —
choix Visa/Mastercard, sélection = liseré encre 1,5px + `{colors.surface}`.

### Splash
Écran immersif plein cadre sur `{colors.green-deep}` (`Brand.greenDeep`), encre
`{colors.deep-ink}` (`Brand.deepInk`, blanc constant dans les deux thèmes).
**Lockup** centré : tuile logo 60px (`LogoMark`), fond blanc à 12 % (`Brand.deepInkFill`),
glyphe blanc ; nom « MoniPay » en `{typography.title-lg}` (`Font.titleLarge`) ; écart
tuile/nom = `Metric.rowVertical` (14px).
**Ligne légale** en bas, `{typography.caption}` (`Font.micro`), blanc à 45 %
(`Brand.deepInkMuted`), `Metric.section` au-dessus de la zone sûre.
**Séquence** : révélation (fondu + montée 8px `Motion.rise`, `Motion.screen`) → dwell 1,5s
(`SplashModel.hold`) → Bienvenue. Mouvement réduit : ni montée ni fondu, même dwell.
Un seul élément d'accessibilité combiné (tuile + nom + ligne légale).

### Bienvenue (onboarding)
Réf. Revolut / Wise sur Mobbin. Navbar logo seul (pas de « Se connecter » en haut).
**`we-segs`** — segments de progression façon stories (3 barres 3px, capsules) : le segment
courant se **remplit** (`weFill` 4,2s linéaire = temps d'auto-avance), les passés sont pleins.
**`we-deck`** — héros : les 3 cartes d'exemple en **éventail** (296px, offsets ±12/13px,
rotations 4,5°/−4°), la carte de devant se **drague** (suit le doigt + rotation `dx/18`),
relâche > 55px = carte suivante/précédente, tap = suivante ; flottement lent `weBob` en repos.
**`we-texts`** — titres/corps empilés en `grid-area:1/1` (hauteur réservée, zéro saut),
transition fondu + `blur(5px)` + montée 0,5s.
En bas : **deux boutons empilés** — « Créer mon compte » (plein) + « J'ai déjà un compte »
(quiet) — puis la ligne légale. Entrée en cascade `weRise` (délais .05→.26s).

### Graphiques (Analyse)
**`bars-chart`** — barres `{colors.ink}` à 13 %, **active en `{colors.green}`** + étiquette,
grille pointillée `{colors.hairline}`, ticks 9,5px `{colors.ink-3}`. Pousse `barUp` 0,5s en cascade.
**`donut`** — segments 21px (25px sélectionné, non-sélectionnés à 22 %), centre :
libellé 11px + montant 29px + sous-libellé. Balayage `donutIn` 0,8s.
**`cat-row`** — disque catégorie teinté `--cat-*` + jauge `meter`.
**`delta-chip`** — hausse = `{colors.pend}`, baisse = `{colors.credit}` (une hausse de
dépenses n'est pas une erreur, une baisse n'est pas un crédit).
**`cal-grid`** — calendrier : jour à point vert = dépense, sélection = fond
`{colors.green}` ; mois en chips (`.on` = encre).
**`fchip`** — filtres : pastille teintée + libellé, `.on` = fond encre.

### Vocabulaire du domaine
`KIND_LABELS` : Paiement carte · Rechargement · Conversion · Remboursement · Frais ·
Transfert. `STATUS_META` : Réussi (credit/check) · En attente (pend/clock) · Refusé
(debit/x) · Remboursé (neutral/undo).

## Motion

Tout est conditionné par `.anim` sur la racine ; `prefers-reduced-motion` neutralise tout.

| Effet | Durée / courbe |
|---|---|
| Entrée d'onglet (`riseIn` en cascade) | 0,38s, délais 0,02→0,20s |
| Poussée de pile | 260ms `cubic-bezier(.32,.72,.28,1.02)` |
| Feuille | 380ms `cubic-bezier(.26,1.06,.34,1)` |
| Menu widget | 360ms `cubic-bezier(.3,1.25,.4,1)` |
| Étape de flux `stepFwd/stepBack` | 0,3s |
| Frappe `digitPop` | 0,2s, scale 1.045 |
| Barres / anneau / jauges | 0,5s / 0,8s / 0,8s, délais `--i` |
| Coche : tracé + onde | 0,4s + 0,7s |
| Gel : voile `frostIn` + givre `iceGrow` | 0,55s + 0,8s en cascade (.1/.28/.5s) |
| Révélation `panRoll` (PAN + secrets) | 0,45s `cubic-bezier(.25,.9,.3,1)`, secrets +0,08s |
| Dépliage des détails `.cd-collapse` | 0,42s `cubic-bezier(.3,.8,.3,1)` (grid-rows 0fr→1fr) |
| Anneau de plafond | 0,9s `cubic-bezier(.3,.7,.3,1)` (stroke-dashoffset) |
| Entrée du détail de carte `cdHero` | 0,5s, uniquement au push (`[data-anim="push"]`) |
| Splash : révélation (fondu + montée 8px) | `Motion.screen` — 0,35s ease-in-out |
| Splash : dwell avant Bienvenue | 1,5s (`SplashModel.hold`) |
| Bienvenue : segment `weFill` | 4,2s linéaire (= dwell d'auto-avance) |
| Bienvenue : rotation du deck | 0,55s `cubic-bezier(.3,1.25,.4,1)` (ressort), drag sans transition |
| Bienvenue : flottement `weBob` | 3,2s ease-in-out alternate |
| Bienvenue : textes (fondu + blur + montée) | 0,5s ease |
| Bienvenue : entrée `weRise` en cascade | 0,55s `cubic-bezier(.2,.7,.3,1)`, délais .05→.26s |
| Retours de pression | scale .92–.985, 0,12–0,14s |
| Bascule de thème `.theme-anim` | fondu 0,3s |

> **Mécanique à ne pas « corriger »** : `F.dir` (direction d'étape) et `F.bump`
> (pulsation) sont **consommés au rendu** (`flowWrap()`, `consumeBump()`) pour ne jouer
> qu'une fois. Les actions les posent, le rendu les efface.

## Do's and Don'ts

### Do
- Définir toute nouvelle couleur dans **les deux** blocs sombres (média + `data-theme`).
- Réserver `{colors.green}` aux actions ; `{colors.green-deep}` aux panneaux ;
  `{colors.accent-bright}` au contenu posé **sur** vert profond.
- Composer les montants avec `.money` (+`.tnum`), séparateur U+202F, unité en suffixe atténué.
- Accompagner chaque état d'une icône **et** d'un libellé (`pill`, `tl`, `STATUS_META`).
- Poser les listes sur le papier, séparées par `.rule.inset` — la respiration structure.
- Redescendre en aplats `{colors.well}` à l'intérieur d'une feuille (pas de verre sur verre).
- Utiliser de vrais logos SVG inline sur pastille blanche `{colors.chip-ring}`.
- Ancrer la créativité dans la région (Héritage : ndop, bogolan, wax) plutôt que copier.

### Don't
- Pas de vert en fond de page ; le vert profond est un panneau, pas un thème.
- Pas de blanc sur `{colors.green}` en mode sombre — le texte y est vert profond #04301F.
- Pas de deuxième famille de police ; pas de capitales hors marquages gravés de la carte
  (VIRTUELLE, EXPIRE, CVV) ; pas de poids > 600.
- Pas d'emoji dans l'UI ; pas de faux logos.
- Pas de couleurs de statut comme teintes de série dans les graphiques.
- Pas de dégradés décoratifs ; la matière vient du guilloché et du verre.
- Pas de cartes blanches autour des listes ; pas de padding horizontal en dur (`.gutter`).
- Pas de retouche de la palette `--cat-*` sans repasser `validate_palette.js`.
- Pas d'états hover : le prototype est mobile-first — Default et Active/Pressed seulement.

## Responsive Behavior

| Contexte | Comportement |
|---|---|
| ≥ 900px | Atelier : rail 264px + téléphone 393×852 centré (échelle .9 sous 960px de haut, .82 sous 860px) |
| < 900px | Plein écran mobile : rail en tiroir (bouton « Plan »), device sans cadre ni coins |

- Cibles tactiles : boutons 52px, lignes 56px, nav 38px, touches 52px.
- Le contenu défile **sous** la barre d'onglets flottante (`{spacing.tab-clearance}`).
- Les carrousels (cartes, sélecteur coverflow) : scroll horizontal + snap, jamais de wrap.
- `.scroll` gère le défilement interne ; le body ne défile jamais (`overscroll-behavior:contain`).

## Agent Prompt Guide

**Référence rapide** : action = `{colors.green}` · texte sur action = `{colors.on-green}`
(sombre : #04301F) · panneau = `{colors.green-deep}` · entrée = `{colors.credit}` ·
sortie = `{colors.debit}` · attente = `{colors.pend}` · fond = `{colors.paper}` ·
creux = `{colors.well}` · capsule = 999px · gutter = 20px.

**Prompts prêts à l'emploi :**
- « Ajoute un écran [X] : fond `{colors.paper}`, navbar 52px avec boutons de verre,
  listes en `.row` séparées par `.rule.inset`, CTA `button-primary` capsule 52px. »
- « Ajoute une feuille [X] : `.sheet` en verre, poignée, titre 19px ; contrôles internes
  en aplats `{colors.well}` (jamais de verre sur verre). »
- « Ajoute un état [X] : `pill` avec icône + libellé, couleurs `credit|debit|pend`. »
- « Ajoute un habillage de carte : aplat fixe + encre dérivée dans `CARD_THEMES`,
  motif éventuel via `cardArt()` ton sur ton, lisibilité de la marque d'abord. »

**Rituel de vérification (après toute modif) :**
1. `node --check app.js` puis `python3 build.py` (assemble `dist/index.html`).
2. QA Chrome sur `http://localhost:8742/index.html?v=N` (incrémenter N + hard reload) ;
   piloter en JS (`PHASE="app"; renderPhone(); ACTIONS.openConvert()`).
3. Tester **clair ET sombre**, remettre le thème sur **Auto**.
4. Republier l'artifact (même URL), commit + push en français.

## Known Gaps

- **Deux systèmes coexistent** : l'app SwiftUI (`MoneyPay/`) porte un langage antérieur
  (« Registre », monochrome) — obsolète, ne fait pas foi, à l'exception du splash, désormais
  porté par les jetons partagés.
- Google Sans Flex est servie par Google Fonts ; hors ligne, le fallback système change
  sensiblement la voix typographique.
- `rounded: 20px` (`.method-card`, `.fx-card`) est hors barème — assumé, non tokenisé.
- Les états hover n'existent pas (mobile-first) ; un portage desktop devrait les définir.
- Non tranchés : galerie « toutes les collections » en grille avant le carrousel ;
  keypad à opérateurs (+ − × ÷) façon Revolut ; étape confirm sur Envoyer.
- Les icônes sont un jeu SVG interne à `app.js` (traits 1,8–2px) — non formalisées en
  tokens ici.
