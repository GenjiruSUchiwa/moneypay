# Handoff — MoniPay, refonte de la page Analyse (session du 27–28/08/2026)

Reprise pour un agent frais. Lire EN PREMIER **`HANDOFF.md`** (contexte projet, architecture JS,
rituel de vérification, style de collaboration), puis **`HANDOFF-2026-08-27.md`** (DA Liquid Glass).
Ce document ne couvre que la présente session.

## Travail accompli (tout committé & poussé sur `main`, artifact à jour)

- `d17a86d` — **Refonte Analyse v1** (réf. Revolut/Wise/Family via Mobbin) : héro piloté par la
  sélection (compteur animé, chip delta), bascule histogramme ⇄ anneau, anneau interactif
  (tap segment → détail au centre, autres atténués), catégories restylées.
- `3dfa146` — **Analyse v2** : périodes réelles **Semaine / Mois / Année** (données journalières
  d'août dans `S.days`, 27 jours, somme = 108 500), **calendrier custom** (réf. vidéo Moniwa
  `~/Downloads/IMG_3312.MP4`, frames ~28–44 s : bande de mois, grille L→D, points d'intensité,
  résumé, tap jour), **sheet Filtres** (chips catégories + cartes, Réinitialiser/Appliquer,
  badge sur l'entonnoir), anneau agrandi (R 82, texte central aéré), pastilles de catégories
  **pleines** (disque couleur + glyphe blanc), sheets à hauteur de contenu (`.sheet.fit`).
- `256feca` — **Répartition regroupable** : l'en-tête « Par catégorie ⌄ » ouvre un menu verre
  « Regrouper par » (Catégorie / Marchand / Carte) via `showDialog`. `spendGroups()` agrège les
  txs filtrées ; section « Par carte » séparée supprimée.

## Points techniques clés (nouveaux cette session)

1. `insSeries()` remplace `insMonths()` : retourne `{xaf, label, when, short}` selon `F.insPeriod`
   (0 = 7 derniers jours, 1 = jours d'août, 2 = 12 mois). Échelle dynamique via `niceMax()`.
2. Filtres : `F.insFCats` (array), `F.insFCard` — dans `F_KEEP`, appliqués dans `spendByCat()`,
   `spendGroups()` et via `insFilterRatio()` (les barres sont **proportionnellement réduites**,
   pas recalculées jour par jour — ponytail assumé, à revoir si données réelles).
3. Regroupement : `F.insGroup` (0 cat / 1 marchand / 2 carte), dans `F_KEEP`.
4. Calendrier : `F.calM` (index dans `S.months`, 11 = août), `F.calD` ; `calDaySpend()` utilise
   `S.days` pour août, motif pseudo-aléatoire stable ailleurs. Bande de mois auto-centrée
   (hook dans `renderPhone`, `#cal-strip .on`).
5. `sectionHead(..., { chev: "chevD" })` accepte désormais un nom d'icône pour le chevron.
6. ⚠ Outil Chrome MCP : `wait` **gèle l'horloge d'animation** de la page — un anneau « à moitié
   balayé » ou une sheet invisible sur une capture est un artefact ; recapturer au call suivant.

## État

- Artifact **à jour** : https://claude.ai/code/artifact/b1804529-24e7-4acc-90d0-e5db23f7bfdf
  (republication de `prototype/dist/index.html` en passant `url` ; la même session republie sans friction).
- Build : `python3 build.py` dans `prototype/` (dist ≈ 244 Ko). Serveur : `python3 -m http.server 8742`
  dans `prototype/` ; onglet Chrome `http://localhost:8742/index.html?v=52` (incrémenter `?v=` +
  hard reload `cmd+shift+r`). Raccourci QA : `PHASE="main"; TAB="insights"; renderPhone("tab")` via
  `javascript_tool`.
- Thème remis sur **Auto** (rituel respecté ; `applyTheme("auto")` — la préférence vit dans
  `localStorage["mp-theme"]`).
- Idées non tranchées : cf. « Points ouverts » de `HANDOFF.md` ; en plus — indicateur de sélection
  dans le menu « Regrouper par » (pas d'état « actif » dans `showDialog`), transactions du jour
  sous le calendrier (Moniwa en affiche), second déclencheur `+` de la vidéo toujours non demandé.

## Compétences suggérées (Skill tool)

- `design` — charger AVANT toute nouvelle UI (utilisée toute la session).
- `dataviz` — si retouche de palette/graphiques (valider au script, jamais à l'œil).
- Outils : Chrome MCP (QA via `javascript_tool`, coordonnées peu fiables), ffmpeg (frames vidéo),
  Mobbin MCP en secours. Ponytail actif (mode full) : diff minimal, réponses < 500 mots.

## Style de collaboration (rappel court)

Français, itérations courtes : analyser les références visuelles de l'utilisateur AVANT de coder →
modifier → vérifier dans Chrome (clair ET sombre) → rebuild → commit/push en français SANS
redemander → republier l'artifact même URL → thème sur Auto → captures envoyées à l'utilisateur.
